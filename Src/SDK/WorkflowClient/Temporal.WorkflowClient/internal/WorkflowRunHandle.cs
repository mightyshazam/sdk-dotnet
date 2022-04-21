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

        private bool _isOwnerWorkflowExposed;
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

            _ownerWorkflow = null;
            _getOwnerWorkflowCompletion = null;
            _isOwnerWorkflowExposed = false;
        }

        internal WorkflowRunHandle(WorkflowHandle workflow, string workflowRunId)
        {
            Validate.NotNull(workflow);

            try
            {
                workflow.ValidateIsBound();
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"{nameof(workflow)} must be bound in order to use it for"
                                          + $" constructing a {nameof(WorkflowRunHandle)}.",
                                            ex);
            }

            Validate.NotNull(workflow.TemporalServiceClient);
            Validate.NotNullOrWhitespace(workflow.TemporalServiceClient.Namespace);
            ValidateWorkflowProperty.WorkflowId(workflow.WorkflowId);

            ValidateWorkflowProperty.RunId.Specified(workflowRunId);

            if (!workflow.TemporalServiceClient.Namespace.Equals(workflow.Namespace))
            {
                throw new InvalidOperationException("TemporalServiceClient and Workflow namespaces do not match. This is an SDK bug."
                                                  + "Please report:  https://github.com/temporalio/sdk-dotnet/issues");
            }

            _temporalClient = workflow.TemporalServiceClient;
            WorkflowId = workflow.WorkflowId;
            WorkflowRunId = workflowRunId;

            _ownerWorkflow = workflow;
            _getOwnerWorkflowCompletion = Task.FromResult((IWorkflowHandle) workflow);
            _isOwnerWorkflowExposed = true;
        }

        #region --- APIs to access basic workflow run details ---

        public string Namespace { get { return _temporalClient.Namespace; } }
        public string WorkflowId { get; }
        public string WorkflowRunId { get; }

        public Task<IWorkflowHandle> GetOwnerWorkflowAsync(CancellationToken cancelToken = default)
        {
            Task<IWorkflowHandle> getOwnerWorkflowCompletion = _getOwnerWorkflowCompletion;

            // Benign race. Worst case we execute FindOwnerWorkflowAsync(..) multiple times (idempotent).
            if (getOwnerWorkflowCompletion != null)
            {
                _isOwnerWorkflowExposed = true;
                return getOwnerWorkflowCompletion;
            }

            return FindOwnerWorkflowAsync(cancelToken);
        }

        private async Task<IWorkflowHandle> FindOwnerWorkflowAsync(CancellationToken cancelToken = default)
        {
            await _temporalClient.EnsureConnectedAsync(cancelToken);

            WorkflowHandle invokerPipelineProvider = GetOrCreateServiceInvocationPipelineProvider();
            ITemporalClientInterceptor invokerPipeline = invokerPipelineProvider.GetOrCreateServiceInvocationPipeline();
            GetWorkflowChainId.Result resLatestWfChain = await invokerPipeline.GetWorkflowChainIdAsync(
                                                                new GetWorkflowChainId.Arguments(Namespace,
                                                                                                 WorkflowId,
                                                                                                 WorkflowRunId,
                                                                                                 cancelToken));
            TrySetOwnerWorkflow(invokerPipelineProvider, resLatestWfChain);

            IWorkflowHandle ownerWorkflow = Volatile.Read(ref _ownerWorkflow);
            if (ownerWorkflow == null)
            {
                throw new InvalidOperationException($"Failed to {nameof(FindOwnerWorkflowAsync)}(..)."
                                                      + $" This may be an SDK bug."
                                                      + $" Please report: https://github.com/temporalio/sdk-dotnet/issues.");
            }

            _isOwnerWorkflowExposed = true;
            return ownerWorkflow;
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

            WorkflowHandle invokerPipelineProvider = GetOrCreateServiceInvocationPipelineProvider();
            ITemporalClientInterceptor invokerPipeline = invokerPipelineProvider.GetOrCreateServiceInvocationPipeline();
            DescribeWorkflow.Result resDesrc = await invokerPipeline.DescribeWorkflowAsync(
                                                                new DescribeWorkflow.Arguments(Namespace,
                                                                                                WorkflowId,
                                                                                                WorkflowChainId: null,
                                                                                                WorkflowRunId,
                                                                                                throwIfWorkflowNotFound,
                                                                                                cancelToken));
            TrySetOwnerWorkflow(invokerPipelineProvider, resDesrc);
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

            WorkflowHandle invokerPipelineProvider = GetOrCreateServiceInvocationPipelineProvider();
            ITemporalClientInterceptor invokerPipeline = invokerPipelineProvider.GetOrCreateServiceInvocationPipeline();
            IWorkflowRunResult resWfRun = await invokerPipeline.AwaitConclusionAsync(new AwaitConclusion.Arguments(Namespace,
                                                                                                                   WorkflowId,
                                                                                                                   WorkflowChainId: null,
                                                                                                                   WorkflowRunId,
                                                                                                                   FollowWorkflowChain: false,
                                                                                                                   cancelToken));
            TrySetOwnerWorkflow(invokerPipelineProvider, resWfRun);

            if (resWfRun != null && resWfRun is AwaitConclusion.Result resAwaitWfConcl)
            {
                // If we have a known implementation of IWorkflowRunResult, then set its WorkflowChain to this chain so that the
                // result's TryGetContinuationRun(..) method can create the respective run-handle using the right chain-handle
                // and client instances.

                IWorkflowHandle ownerWorkflow = Volatile.Read(ref _ownerWorkflow);
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

            signalConfig = signalConfig ?? SignalWorkflowConfiguration.Default;

            WorkflowHandle invokerPipelineProvider = GetOrCreateServiceInvocationPipelineProvider();
            ITemporalClientInterceptor invokerPipeline = invokerPipelineProvider.GetOrCreateServiceInvocationPipeline();
            SignalWorkflow.Result resSigWf = await invokerPipeline.SignalWorkflowAsync(
                                                                new SignalWorkflow.Arguments<TSigArg>(Namespace,
                                                                                                      WorkflowId,
                                                                                                      WorkflowChainId: null,
                                                                                                      WorkflowRunId,
                                                                                                      signalName,
                                                                                                      signalArg,
                                                                                                      signalConfig,
                                                                                                      cancelToken));
            TrySetOwnerWorkflow(invokerPipelineProvider, resSigWf);
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

            queryConfig = queryConfig ?? QueryWorkflowConfiguration.Default;

            WorkflowHandle invokerPipelineProvider = GetOrCreateServiceInvocationPipelineProvider();
            ITemporalClientInterceptor invokerPipeline = invokerPipelineProvider.GetOrCreateServiceInvocationPipeline();
            QueryWorkflow.Result<TResult> resQryWf = await invokerPipeline.QueryWorkflowAsync<TQryArg, TResult>(
                                                                new QueryWorkflow.Arguments<TQryArg>(Namespace,
                                                                                                     WorkflowId,
                                                                                                     WorkflowChainId: null,
                                                                                                     WorkflowRunId,
                                                                                                     queryName,
                                                                                                     queryArg,
                                                                                                     queryConfig,
                                                                                                     cancelToken));
            TrySetOwnerWorkflow(invokerPipelineProvider, resQryWf);
            return resQryWf.Value;
        }

        public async Task RequestCancellationAsync(CancellationToken cancelToken = default)
        {
            await _temporalClient.EnsureConnectedAsync(cancelToken);

            WorkflowHandle invokerPipelineProvider = GetOrCreateServiceInvocationPipelineProvider();
            ITemporalClientInterceptor invokerPipeline = invokerPipelineProvider.GetOrCreateServiceInvocationPipeline();
            RequestCancellation.Result resReqCnclWf = await invokerPipeline.RequestCancellationAsync(
                                                                new RequestCancellation.Arguments(Namespace,
                                                                                                  WorkflowId,
                                                                                                  WorkflowChainId: null,
                                                                                                  WorkflowRunId,
                                                                                                  cancelToken));
            TrySetOwnerWorkflow(invokerPipelineProvider, resReqCnclWf);
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

            WorkflowHandle invokerPipelineProvider = GetOrCreateServiceInvocationPipelineProvider();
            ITemporalClientInterceptor invokerPipeline = invokerPipelineProvider.GetOrCreateServiceInvocationPipeline();
            TerminateWorkflow.Result resTermWf = await invokerPipeline.TerminateWorkflowAsync(
                                                                new TerminateWorkflow.Arguments<TTermArg>(Namespace,
                                                                                                          WorkflowId,
                                                                                                          WorkflowChainId: null,
                                                                                                          WorkflowRunId,
                                                                                                          reason,
                                                                                                          details,
                                                                                                          cancelToken));
            TrySetOwnerWorkflow(invokerPipelineProvider, resTermWf);
        }
        #endregion --- APIs to interact with the workflow run ---

        #region --- Dispose ---
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_isOwnerWorkflowExposed)
                {
                    _ownerWorkflow?.Dispose();
                }
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

        private bool TrySetOwnerWorkflow(WorkflowHandle bindingOperatonPipelineProvider, IWorkflowChainBindingResult bindingResult)
        {
            if (bindingOperatonPipelineProvider != null && !bindingOperatonPipelineProvider.IsBound)
            {
                if (bindingOperatonPipelineProvider.TryApplyBindingUnsafe(bindingResult)
                        && Concurrent.TrySetIfNull(ref _ownerWorkflow, bindingOperatonPipelineProvider))
                {
                    _getOwnerWorkflowCompletion = Task.FromResult((IWorkflowHandle) _ownerWorkflow);
                    return true;
                }

                bindingOperatonPipelineProvider.Dispose();
            }

            return false;
        }

        private WorkflowHandle GetOrCreateServiceInvocationPipelineProvider()
        {
            WorkflowHandle ownerWorkflow = Volatile.Read(ref _ownerWorkflow);
            if (ownerWorkflow == null)
            {
                ownerWorkflow = new WorkflowHandle(_temporalClient, WorkflowId);
            }

            return ownerWorkflow;
        }

        #endregion --- Privates ---
    }
}
