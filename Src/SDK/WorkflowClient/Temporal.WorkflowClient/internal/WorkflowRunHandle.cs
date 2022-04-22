using System;
using System.Threading;
using System.Threading.Tasks;
using Temporal.Api.Enums.V1;
using Temporal.Api.WorkflowService.V1;
using Temporal.Common;
using Temporal.Util;
using Temporal.WorkflowClient.Interceptors;
using Temporal.WorkflowClient.OperationConfigurations;

namespace Temporal.WorkflowClient
{
    internal class WorkflowRunHandle : IWorkflowRunHandle, IDisposable
    {
        private readonly TemporalClient _temporalClient;

        private readonly object _internalInitLock = new object();

        private ITemporalClientInterceptor _serviceInvocationPipeline;

        private string _workflowChainId;
        private WorkflowHandle _ownerWorkflow;
        private Task<IWorkflowHandle> _getOwnerWorkflowCompletion;

        internal WorkflowRunHandle(TemporalClient temporalClient, string workflowId, string workflowRunId)
        {
            Validate.NotNull(temporalClient);
            Validate.NotNullOrWhitespace(temporalClient.Namespace);
            ValidateWorkflowProperty.WorkflowId(workflowId);
            ValidateWorkflowProperty.RunId.Specified(workflowRunId);

            _temporalClient = temporalClient;
            WorkflowId = workflowId;
            WorkflowRunId = workflowRunId;

            _serviceInvocationPipeline = null;

            _workflowChainId = null;
            _ownerWorkflow = null;
            _getOwnerWorkflowCompletion = null;
        }

        internal WorkflowRunHandle(WorkflowHandle workflow, string workflowRunId)
        {
            Validate.NotNull(workflow);
            Validate.NotNull(workflow.TemporalServiceClient);
            Validate.NotNullOrWhitespace(workflow.TemporalServiceClient.Namespace);
            ValidateWorkflowProperty.WorkflowId(workflow.WorkflowId);
            ValidateWorkflowProperty.RunId.Specified(workflowRunId);

            try
            {
                Validate.NotNull(workflow.GetServiceInvocationPipeline());
                ValidateWorkflowProperty.ChainId.Bound(workflow.WorkflowChainId);
            }
            catch (InvalidOperationException invOpEx)
            {
                throw new ArgumentException($"{nameof(workflow)} must be bound in order to use it for"
                                          + $" constructing a {nameof(WorkflowRunHandle)}.",
                                            invOpEx);
            }

            if (!workflow.TemporalServiceClient.Namespace.Equals(workflow.Namespace))
            {
                throw new InvalidOperationException("TemporalServiceClient and Workflow namespaces do not match. This is an SDK bug."
                                                  + "Please report:  https://github.com/temporalio/sdk-dotnet/issues");
            }

            _temporalClient = workflow.TemporalServiceClient;
            WorkflowId = workflow.WorkflowId;
            WorkflowRunId = workflowRunId;

            _serviceInvocationPipeline = workflow.GetServiceInvocationPipeline();

            _workflowChainId = workflow.WorkflowChainId;
            _ownerWorkflow = workflow;
            _getOwnerWorkflowCompletion = Task.FromResult((IWorkflowHandle) workflow);
        }

        #region --- APIs to access basic workflow run details ---

        public string Namespace { get { return _temporalClient.Namespace; } }
        public string WorkflowId { get; }
        public string WorkflowRunId { get; }

        public Task<IWorkflowHandle> GetOwnerWorkflowAsync(CancellationToken cancelToken = default)
        {
            Task<IWorkflowHandle> getOwnerWorkflowCompletion = _getOwnerWorkflowCompletion;
            if (getOwnerWorkflowCompletion != null)
            {
                return getOwnerWorkflowCompletion;
            }

            return FindOwnerWorkflowAsync(cancelToken);
        }

        private async Task<IWorkflowHandle> FindOwnerWorkflowAsync(CancellationToken cancelToken = default)
        {
            string workflowChainId = _workflowChainId;
            if (workflowChainId != null)
            {
                return GetOrCreateOwnerWorkflowHandle();
            }

            await _temporalClient.EnsureConnectedAsync(cancelToken);

            GetWorkflowChainId.Arguments opArgs = new(Namespace,
                                                      WorkflowId,
                                                      WorkflowRunId,
                                                      cancelToken);

            ITemporalClientInterceptor invokerPipeline = GetOrCreateServiceInvocationPipeline(opArgs);
            GetWorkflowChainId.Result resLatestWfChain = await invokerPipeline.GetWorkflowChainIdAsync(opArgs);
            TrySetOwnerChainId(resLatestWfChain);

            workflowChainId = Volatile.Read(ref _workflowChainId);
            if (workflowChainId == null)
            {
                throw new InvalidOperationException($"Failed to {nameof(FindOwnerWorkflowAsync)}(..)."
                                                      + $" This may be an SDK bug."
                                                      + $" Please report: https://github.com/temporalio/sdk-dotnet/issues.");
            }
            else
            {
                return GetOrCreateOwnerWorkflowHandle();
            }
        }

        #endregion --- APIs to access basic workflow run details ---

        #region --- APIs to describe the workflow run ---

        public async Task<string> GetWorkflowTypeNameAsync(CancellationToken cancelToken = default)
        {
            DescribeWorkflowExecutionResponse resDescrWf = await DescribeAsync(cancelToken);
            return resDescrWf.WorkflowExecutionInfo.Type.Name;
        }

        public async Task<WorkflowExecutionStatus> GetStatusAsync(CancellationToken cancelToken = default)
        {
            DescribeWorkflowExecutionResponse resDescrWf = await DescribeAsync(cancelToken);
            return resDescrWf.WorkflowExecutionInfo.Status;
        }

        public async Task<bool> ExistsAsync(CancellationToken cancelToken = default)
        {
            DescribeWorkflow.Result resDesrc = await DescribeAsync(throwIfWorkflowNotFound: false, cancelToken);

            if (resDesrc.StatusCode == Grpc.Core.StatusCode.OK)
            {
                return true;
            }
            else if (resDesrc.StatusCode == Grpc.Core.StatusCode.NotFound)
            {
                return false;
            }
            else
            {
                throw new InvalidOperationException($"Unexpected {nameof(DescribeWorkflow.Result.StatusCode)}"
                                                  + $" ({resDesrc.StatusCode.ToString()} = {(int) resDesrc.StatusCode})."
                                                  + " This is likely a Temporal SDK bug."
                                                  + " Please report: https://github.com/temporalio/sdk-dotnet/issues");
            }
        }

        public async Task<DescribeWorkflowExecutionResponse> DescribeAsync(CancellationToken cancelToken = default)
        {
            DescribeWorkflow.Result resDesrc = await DescribeAsync(throwIfWorkflowNotFound: true, cancelToken);
            return resDesrc.DescribeWorkflowExecutionResponse;
        }

        private async Task<DescribeWorkflow.Result> DescribeAsync(bool throwIfWorkflowNotFound, CancellationToken cancelToken)
        {
            await _temporalClient.EnsureConnectedAsync(cancelToken);

            DescribeWorkflow.Arguments opArgs = new(Namespace,
                                                    WorkflowId,
                                                    _workflowChainId,
                                                    WorkflowRunId,
                                                    throwIfWorkflowNotFound,
                                                    cancelToken);

            ITemporalClientInterceptor invokerPipeline = GetOrCreateServiceInvocationPipeline(opArgs);
            DescribeWorkflow.Result resDesrc = await invokerPipeline.DescribeWorkflowAsync(opArgs);
            TrySetOwnerChainId(resDesrc);
            return resDesrc;
        }

        #endregion --- APIs to describe the workflow run ---

        #region --- APIs to interact with the workflow run ---

        /// <summary>The returned task completes when this chain finishes (incl. any runs not yet started). Performs long poll.</summary>
        public async Task<TResult> GetResultAsync<TResult>(CancellationToken cancelToken = default)
        {
            IWorkflowRunResult runResult = await AwaitConclusionAsync(cancelToken);
            return runResult.GetValue<TResult>();
        }

        public async Task<IWorkflowRunResult> AwaitConclusionAsync(CancellationToken cancelToken = default)
        {
            await _temporalClient.EnsureConnectedAsync(cancelToken);

            AwaitConclusion.Arguments opArgs = new(Namespace,
                                                   WorkflowId,
                                                   _workflowChainId,
                                                   WorkflowRunId,
                                                   FollowWorkflowChain: false,
                                                   cancelToken);

            ITemporalClientInterceptor invokerPipeline = GetOrCreateServiceInvocationPipeline(opArgs);
            IWorkflowRunResult resWfRun = await invokerPipeline.AwaitConclusionAsync(opArgs);
            TrySetOwnerChainId(resWfRun);

            if (resWfRun != null && resWfRun is AwaitConclusion.Result resAwaitWfConcl)
            {
                // If we have a known implementation of IWorkflowRunResult, then set its WorkflowChain to this chain so that the
                // result's TryGetContinuationRun(..) method can create the respective run-handle using the right chain-handle
                // and client instances.

                IWorkflowHandle ownerWorkflow = _getOwnerWorkflowCompletion?.Result;
                if (ownerWorkflow != null)
                {
                    resAwaitWfConcl.WorkflowChain = ownerWorkflow;
                }
            }

            return resWfRun;
        }

        public Task SignalAsync(string signalName,
                                SignalWorkflowConfiguration signalConfig = null,
                                CancellationToken cancelToken = default)
        {
            return SignalAsync(signalName, Payload.Void, signalConfig, cancelToken);
        }

        public async Task SignalAsync<TSigArg>(string signalName,
                                         TSigArg signalArg,
                                         SignalWorkflowConfiguration signalConfig = null,
                                         CancellationToken cancelToken = default)
        {
            await _temporalClient.EnsureConnectedAsync(cancelToken);

            SignalWorkflow.Arguments<TSigArg> opArgs = new(Namespace,
                                                           WorkflowId,
                                                           _workflowChainId,
                                                           WorkflowRunId,
                                                           signalName,
                                                           signalArg,
                                                           signalConfig ?? SignalWorkflowConfiguration.Default,
                                                           cancelToken);

            ITemporalClientInterceptor invokerPipeline = GetOrCreateServiceInvocationPipeline(opArgs);
            SignalWorkflow.Result resSigWf = await invokerPipeline.SignalWorkflowAsync(opArgs);
            TrySetOwnerChainId(resSigWf);
        }

        public Task<TResult> QueryAsync<TResult>(string queryName,
                                                 QueryWorkflowConfiguration queryConfig = null,
                                                 CancellationToken cancelToken = default)
        {
            return QueryAsync<IPayload.Void, TResult>(queryName, Payload.Void, queryConfig, cancelToken);
        }

        public async Task<TResult> QueryAsync<TQryArg, TResult>(string queryName,
                                                          TQryArg queryArg,
                                                          QueryWorkflowConfiguration queryConfig = null,
                                                          CancellationToken cancelToken = default)

        {
            await _temporalClient.EnsureConnectedAsync(cancelToken);

            QueryWorkflow.Arguments<TQryArg> opArgs = new(Namespace,
                                                          WorkflowId,
                                                          _workflowChainId,
                                                          WorkflowRunId,
                                                          queryName,
                                                          queryArg,
                                                          queryConfig ?? QueryWorkflowConfiguration.Default,
                                                          cancelToken);

            ITemporalClientInterceptor invokerPipeline = GetOrCreateServiceInvocationPipeline(opArgs);
            QueryWorkflow.Result<TResult> resQryWf = await invokerPipeline.QueryWorkflowAsync<TQryArg, TResult>(opArgs);
            TrySetOwnerChainId(resQryWf);
            return resQryWf.Value;
        }

        public async Task RequestCancellationAsync(CancellationToken cancelToken = default)
        {
            await _temporalClient.EnsureConnectedAsync(cancelToken);

            RequestCancellation.Arguments opArgs = new(Namespace,
                                                       WorkflowId,
                                                       _workflowChainId,
                                                       WorkflowRunId,
                                                       cancelToken);

            ITemporalClientInterceptor invokerPipeline = GetOrCreateServiceInvocationPipeline(opArgs);
            RequestCancellation.Result resReqCnclWf = await invokerPipeline.RequestCancellationAsync(opArgs);
            TrySetOwnerChainId(resReqCnclWf);
        }

        public Task TerminateAsync(string reason = null,
                                   CancellationToken cancelToken = default)
        {
            return TerminateAsync(reason, Payload.Void, cancelToken);
        }

        public async Task TerminateAsync<TTermArg>(string reason,
                                             TTermArg details,
                                             CancellationToken cancelToken = default)
        {
            await _temporalClient.EnsureConnectedAsync(cancelToken);

            TerminateWorkflow.Arguments<TTermArg> opArgs = new(Namespace,
                                                               WorkflowId,
                                                               _workflowChainId,
                                                               WorkflowRunId,
                                                               reason,
                                                               details,
                                                               cancelToken);

            ITemporalClientInterceptor invokerPipeline = GetOrCreateServiceInvocationPipeline(opArgs);
            TerminateWorkflow.Result resTermWf = await invokerPipeline.TerminateWorkflowAsync(opArgs);
            TrySetOwnerChainId(resTermWf);
        }
        #endregion --- APIs to interact with the workflow run ---

        #region --- Dispose ---
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        // Uncomment finalizer IFF `Dispose(bool disposing)` has code for freeing unmanaged resources.
        // ~WorkflowHandle()
        // {
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion --- Dispose ---

        #region --- Privates ---

        private bool TrySetOwnerChainId(IWorkflowOperationResult bindingOperationResult)
        {
            // A run handle has a fixed run-id. Thus, any potentially binding operation will always yield he same chain-id.
            // Therefore, the below race is benign.
            if (_workflowChainId == null
                    && bindingOperationResult != null
                    && bindingOperationResult.TryGetBoundWorkflowChainId(out string boundChainId))
            {
                ValidateWorkflowProperty.ChainId.Bound(boundChainId);
                Volatile.Write(ref _workflowChainId, boundChainId);
                return true;
            }

            return false;
        }

        private WorkflowHandle GetOrCreateOwnerWorkflowHandle()
        {
            WorkflowHandle ownerWorkflow = _ownerWorkflow;
            if (ownerWorkflow == null)
            {
                lock (_internalInitLock)
                {
                    ownerWorkflow = _ownerWorkflow;

                    if (ownerWorkflow == null)
                    {
                        string workflowChainId = _workflowChainId;
                        ITemporalClientInterceptor serviceInvocationPipeline = _serviceInvocationPipeline;

                        ownerWorkflow = WorkflowHandle.CreateForRun(_temporalClient, WorkflowId, workflowChainId, serviceInvocationPipeline);
                        _ownerWorkflow = ownerWorkflow;
                        _getOwnerWorkflowCompletion = Task.FromResult((IWorkflowHandle) ownerWorkflow);
                    }
                }
            }

            return ownerWorkflow;
        }

        private ITemporalClientInterceptor GetOrCreateServiceInvocationPipeline(IWorkflowOperationArguments opArgs)
        {
            return _temporalClient.GetOrCreateServiceInvocationPipeline(this,
                                                                        ref _serviceInvocationPipeline,
                                                                        _internalInitLock,
                                                                        opArgs);
        }

        #endregion --- Privates ---
    }
}
