using System;
using System.Threading;
using System.Threading.Tasks;
using Temporal.Util;
using Temporal.Api.Enums.V1;
using Temporal.Api.WorkflowService.V1;
using Temporal.Common;
using Temporal.WorkflowClient.Interceptors;
using Temporal.WorkflowClient.OperationConfigurations;

namespace Temporal.WorkflowClient
{
    internal class WorkflowHandle : IWorkflowHandle, IDisposable
    {
        #region --- Static construction methods ---

        internal static WorkflowHandle CreateUnbound(TemporalClient temporalClient, string workflowId)
        {
            Validate.NotNull(temporalClient);
            ValidateWorkflowProperty.WorkflowId(workflowId);

            return new WorkflowHandle(temporalClient,
                                      workflowId,
                                      workflowChainId: null,
                                      serviceInvocationPipeline: null);
        }

        internal static WorkflowHandle CreateBound(TemporalClient temporalClient, string workflowId, string workflowChainId)
        {
            Validate.NotNull(temporalClient);
            ValidateWorkflowProperty.WorkflowId(workflowId);
            ValidateWorkflowProperty.ChainId.Bound(workflowChainId);

            return new WorkflowHandle(temporalClient,
                                      workflowId,
                                      workflowChainId,
                                      serviceInvocationPipeline: null);
        }

        internal static WorkflowHandle CreateForRun(TemporalClient temporalClient,
                                             string workflowId,
                                             string workflowChainId,
                                             ITemporalClientInterceptor serviceInvocationPipeline)
        {
            Validate.NotNull(temporalClient);
            ValidateWorkflowProperty.WorkflowId(workflowId);
            ValidateWorkflowProperty.ChainId.Bound(workflowChainId);
            Validate.NotNull(serviceInvocationPipeline);

            return new WorkflowHandle(temporalClient,
                                      workflowId,
                                      workflowChainId,
                                      serviceInvocationPipeline);
        }

        #endregion --- Static construction methods ---

        private readonly TemporalClient _temporalClient;

        private SemaphoreSlim _bindigLock = null;
        private string _workflowChainId;
        private bool _isBound;

        private ITemporalClientInterceptor _serviceInvocationPipeline;

        private WorkflowHandle(TemporalClient temporalClient,
                                string workflowId,
                                string workflowChainId,
                                ITemporalClientInterceptor serviceInvocationPipeline)
        {
            Validate.NotNull(temporalClient);
            Validate.NotNullOrWhitespace(temporalClient.Namespace);
            ValidateWorkflowProperty.WorkflowId(workflowId);
            ValidateWorkflowProperty.ChainId.BoundOrUnbound(workflowChainId);

            _temporalClient = temporalClient;

            WorkflowId = workflowId;

            _workflowChainId = workflowChainId;
            _isBound = (workflowChainId != null);

            _serviceInvocationPipeline = serviceInvocationPipeline;
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowHandle.Namespace"/>) for a detailed description.
        /// </summary>
        public string Namespace
        {
            get { return _temporalClient.Namespace; }
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowHandle.WorkflowId"/>) for a detailed description.
        /// </summary>
        public string WorkflowId { get; }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowHandle.IsBound"/>) for a detailed description.
        /// </summary>
        public bool IsBound { get { return Volatile.Read(ref _isBound); } }

        /// <summary>
        /// The <c>WorkflowChainId</c> is the <c>WorkflowRunId</c> of the first run in the workflow chain.<br />
        /// Calling the getter for this property will result in <c>InvalidOperationException</c> if this <c>WorkflowHandle</c> is not bound.
        /// </summary>
        /// <remarks>See the implemented iface API (<see cref="IWorkflowHandle.WorkflowChainId"/>) for a detailed description.</remarks>
        public string WorkflowChainId
        {
            get
            {
                ValidateIsBound();
                return Volatile.Read(ref _workflowChainId);
            }
        }

        internal TemporalClient TemporalServiceClient
        {
            get { return _temporalClient; }
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowHandle.GetWorkflowTypeNameAsync(CancellationToken)"/>) for a detailed description.
        /// </summary>
        public async Task<string> GetWorkflowTypeNameAsync(CancellationToken cancelToken)
        {
            DescribeWorkflowExecutionResponse resDescrWf = await DescribeAsync(cancelToken);
            return resDescrWf.WorkflowExecutionInfo.Type.Name;
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowHandle.GetStatusAsync(CancellationToken)"/>) for a detailed description.
        /// </summary>
        public async Task<WorkflowExecutionStatus> GetStatusAsync(CancellationToken cancelToken)
        {
            DescribeWorkflowExecutionResponse resDescrWf = await DescribeAsync(cancelToken);
            return resDescrWf.WorkflowExecutionInfo.Status;
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowHandle.ExistsAsync(CancellationToken)"/>) for a detailed description.
        /// </summary>        
        public async Task<bool> ExistsAsync(CancellationToken cancelToken)
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

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowHandle.DescribeAsync(CancellationToken)"/>) for a detailed description.
        /// </summary>
        public async Task<DescribeWorkflowExecutionResponse> DescribeAsync(CancellationToken cancelToken)
        {
            DescribeWorkflow.Result resDesrc = await DescribeAsync(throwIfWorkflowNotFound: true, cancelToken);
            return resDesrc.DescribeWorkflowExecutionResponse;
        }

        private async Task<DescribeWorkflow.Result> DescribeAsync(bool throwIfWorkflowNotFound, CancellationToken cancelToken)
        {
            await _temporalClient.EnsureConnectedAsync(cancelToken);

            (SemaphoreSlim bindingLock, string workflowChainId) = IsBound
                                                                    ? (null, WorkflowChainId)
                                                                    : await BeginBindingOperationIfRequiredAsync(cancelToken);
            try
            {
                DescribeWorkflow.Arguments opArgs = new(Namespace,
                                                        WorkflowId,
                                                        workflowChainId,
                                                        WorkflowRunId: null,
                                                        throwIfWorkflowNotFound,
                                                        cancelToken);

                ITemporalClientInterceptor invokerPipeline = GetOrCreateServiceInvocationPipeline(opArgs);
                DescribeWorkflow.Result resDesrc = await invokerPipeline.DescribeWorkflowAsync(opArgs);
                TryApplyBindingIfLockIsHeld(bindingLock, resDesrc);
                return resDesrc;
            }
            finally
            {
                bindingLock?.Release();
            }
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowHandle.EnsureBoundAsync(CancellationToken)"/>) for a
        /// detailed description.
        /// (Design note/future: If not bound && the WorkflowChainBindingPolicy REQUIRES starting new chain - fail telling to use the other overload.)
        /// </summary>        
        public Task EnsureBoundAsync(CancellationToken cancelToken)
        {
            if (IsBound)
            {
                return Task.CompletedTask;
            }

            return BindToLatestChainAsync(cancelToken);
        }

        private async Task<string> BindToLatestChainAsync(CancellationToken cancelToken)
        {
            (SemaphoreSlim bindingLock, string workflowChainId) = IsBound
                                                                    ? (null, WorkflowChainId)
                                                                    : await BeginBindingOperationIfRequiredAsync(cancelToken);
            try
            {
                if (workflowChainId != null)
                {
                    return workflowChainId;
                }

                await _temporalClient.EnsureConnectedAsync(cancelToken);

                GetWorkflowChainId.Arguments opArgs = new(Namespace,
                                                          WorkflowId,
                                                          WorkflowRunId: null,
                                                          cancelToken);

                ITemporalClientInterceptor invokerPipeline = GetOrCreateServiceInvocationPipeline(opArgs);
                GetWorkflowChainId.Result resLatestWfChain = await invokerPipeline.GetWorkflowChainIdAsync(opArgs);

                string chainId = resLatestWfChain?.WorkflowChainId;

                if (!TryApplyBindingIfLockIsHeld(bindingLock, resLatestWfChain))
                {
                    throw new InvalidOperationException($"Failed to {nameof(BindToLatestChainAsync)}(..)."
                                                      + $" ({nameof(chainId)}={chainId.QuoteOrNull()})."
                                                      + $" This may be an SDK bug."
                                                      + $" Please report: https://github.com/temporalio/sdk-dotnet/issues.");
                }

                return chainId;
            }
            finally
            {
                bindingLock?.Release();
            }
        }

        #region StartAsync(..)

        /// <summary>
        /// See the implemented iface API (
        /// <see cref="IWorkflowHandle.StartAsync{TWfArg}(String, String, TWfArg, StartWorkflowConfiguration, Boolean, CancellationToken)"/>
        /// ) for a detailed description.
        /// </summary>
        public async Task<StartWorkflow.Result> StartAsync<TWfArg>(string workflowTypeName,
                                                                   string taskQueue,
                                                                   TWfArg workflowArg,
                                                                   StartWorkflowConfiguration workflowConfig = null,
                                                                   bool throwIfWorkflowChainAlreadyExists = true,
                                                                   CancellationToken cancelToken = default)
        {
            ValidateIsNotBound();
            await _temporalClient.EnsureConnectedAsync(cancelToken);

            SemaphoreSlim bindingLock = GetOrCreateBindingLock();
            await bindingLock.WaitAsync(cancelToken);
            try
            {
                ValidateIsNotBound();

                StartWorkflow.Arguments.StartOnly<TWfArg> opArgs = new(Namespace,
                                                                       WorkflowId,
                                                                       workflowTypeName,
                                                                       taskQueue,
                                                                       workflowArg,
                                                                       workflowConfig ?? StartWorkflowConfiguration.Default,
                                                                       throwIfWorkflowChainAlreadyExists,
                                                                       cancelToken);

                ITemporalClientInterceptor invokerPipeline = GetOrCreateServiceInvocationPipeline(opArgs);
                StartWorkflow.Result resStartWf = await invokerPipeline.StartWorkflowAsync(opArgs);

                if (resStartWf.TryGetBoundWorkflowChainId(out string boundChainId))
                {
                    Bind(boundChainId);
                }

                return resStartWf;
            }
            finally
            {
                bindingLock.Release();
            }
        }

        #endregion StartAsync(..)

        #region SignalWithStartAsync(..)

        /// <summary>
        /// See the implemented iface API (
        /// <see cref="IWorkflowHandle.SignalWithStartAsync{TWfArg, TSigArg}(String, String, TWfArg, String, TSigArg, StartWorkflowConfiguration, CancellationToken)"/>
        /// ) for a detailed description.
        /// </summary>
        public async Task<StartWorkflow.Result> SignalWithStartAsync<TWfArg, TSigArg>(string workflowTypeName,
                                                                                      string taskQueue,
                                                                                      TWfArg workflowArg,
                                                                                      string signalName,
                                                                                      TSigArg signalArg,
                                                                                      StartWorkflowConfiguration workflowConfig = null,
                                                                                      CancellationToken cancelToken = default)
        {
            ValidateIsNotBound();
            await _temporalClient.EnsureConnectedAsync(cancelToken);

            SemaphoreSlim bindingLock = GetOrCreateBindingLock();
            await bindingLock.WaitAsync(cancelToken);
            try
            {
                ValidateIsNotBound();

                StartWorkflow.Arguments.WithSignal<TWfArg, TSigArg> opArgs = new(Namespace,
                                                                                 WorkflowId,
                                                                                 workflowTypeName,
                                                                                 taskQueue,
                                                                                 workflowArg,
                                                                                 signalName,
                                                                                 signalArg,
                                                                                 workflowConfig ?? StartWorkflowConfiguration.Default,
                                                                                 cancelToken);

                ITemporalClientInterceptor invokerPipeline = GetOrCreateServiceInvocationPipeline(opArgs);
                StartWorkflow.Result resStartWf = await invokerPipeline.SignalWorkflowWithStartAsync(opArgs);

                if (resStartWf.TryGetBoundWorkflowChainId(out string boundChainId))
                {
                    Bind(boundChainId);
                }

                return resStartWf;
            }
            finally
            {
                bindingLock.Release();
            }
        }

        #endregion SignalWithStartAsync(..)


        #region --- GetXxxRunAsync(..) APIs to access a specific run ---

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowHandle.CreateRunHandle(String)"/>) for a detailed description.
        /// </summary>
        public IWorkflowRunHandle CreateRunHandle(string workflowRunId)
        {
            Validate.NotNullOrWhitespace(workflowRunId);
            ValidateIsBound();

            return new WorkflowRunHandle(this, workflowRunId);
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowHandle.GetFirstRunAsync(CancellationToken)"/>) for a detailed description.
        /// </summary>
        public async Task<IWorkflowRunHandle> GetFirstRunAsync(CancellationToken cancelToken = default)
        {
            if (IsBound)
            {
                return CreateRunHandle(WorkflowChainId);
            }

            string chainId = await BindToLatestChainAsync(cancelToken);
            return CreateRunHandle(chainId);
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowHandle.GetLatestRunAsync(CancellationToken)"/>) for a detailed description.
        /// </summary>
        public async Task<IWorkflowRunHandle> GetLatestRunAsync(CancellationToken cancelToken = default)
        {
            DescribeWorkflowExecutionResponse resDescrWf = await DescribeAsync(cancelToken);
            return CreateRunHandle(resDescrWf.WorkflowExecutionInfo.Execution.RunId);
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowHandle.TryGetFinalRunAsync(CancellationToken)"/>) for a detailed description.
        /// </summary>
        public async Task<TryResult<IWorkflowRunHandle>> TryGetFinalRunAsync(CancellationToken cancelToken = default)
        {
            DescribeWorkflowExecutionResponse resDescrWf = await DescribeAsync(cancelToken);
            WorkflowExecutionStatus status = resDescrWf.WorkflowExecutionInfo.Status;

            if (status.IsTerminal())
            {
                IWorkflowRunHandle finalRun = CreateRunHandle(resDescrWf.WorkflowExecutionInfo.Execution.RunId);
                return new TryResult<IWorkflowRunHandle>(finalRun);
            }
            else
            {
                return new TryResult<IWorkflowRunHandle>(isSuccess: false, null);
            }
        }

        #endregion --- GetXxxRunAsync(..) APIs to access a specific run ---

        // <summary>Lists runs of this chain only. Needs overloads with filters? Not in V-Alpha.</summary>
        // @ToDo
        //Task<IPaginatedReadOnlyCollectionPage<IWorkflowRunHandle>> ListRunsAsync(NeedsDesign oneOrMoreArgs);

        #region --- APIs to interact with the chain ---

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowHandle.GetResultAsync{TResult}(CancellationToken)"/>)
        /// for a detailed description.
        /// </summary>
        public async Task<TResult> GetResultAsync<TResult>(CancellationToken cancelToken = default)
        {
            IWorkflowRunResult finalRunResult = await AwaitConclusionAsync(cancelToken);
            return finalRunResult.GetValue<TResult>();
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowHandle.AwaitConclusionAync(CancellationToken)"/>) for a detailed description.
        /// </summary>
        public async Task<IWorkflowRunResult> AwaitConclusionAsync(CancellationToken cancelToken)
        {
            await _temporalClient.EnsureConnectedAsync(cancelToken);

            (SemaphoreSlim bindingLock, string workflowChainId) = IsBound
                                                                    ? (null, WorkflowChainId)
                                                                    : await BeginBindingOperationIfRequiredAsync(cancelToken);
            try
            {
                AwaitConclusion.Arguments opArgs = new(Namespace,
                                                       WorkflowId,
                                                       workflowChainId,
                                                       WorkflowRunId: null,
                                                       FollowWorkflowChain: true,
                                                       cancelToken);

                ITemporalClientInterceptor invokerPipeline = GetOrCreateServiceInvocationPipeline(opArgs);
                IWorkflowRunResult resWfRun = await invokerPipeline.AwaitConclusionAsync(opArgs);
                TryApplyBindingIfLockIsHeld(bindingLock, resWfRun);

                if (resWfRun != null && resWfRun is AwaitConclusion.Result resAwaitWfConcl && IsBound)
                {
                    // If we have a known implementation of IWorkflowRunResult, then set its WorkflowChain to this chain so that the
                    // result's TryGetContinuationRun(..) method can create the respective run-handle using the right chain-handle
                    // and client instances.

                    resAwaitWfConcl.WorkflowChain = this;
                }

                return resWfRun;
            }
            finally
            {
                bindingLock?.Release();
            }
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowHandle.SignalAsync(String, CancellationToken)"/>) for a detailed description.
        /// </summary>
        public Task SignalAsync(string signalName,
                                SignalWorkflowConfiguration signalConfig = null,
                                CancellationToken cancelToken = default)
        {
            return SignalAsync(signalName, Payload.Void, signalConfig, cancelToken);
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowHandle.SignalAsync{TSigArg}(String, TSigArg, CancellationToken)"/>)
        /// for a detailed description.
        /// </summary>
        public async Task SignalAsync<TSigArg>(string signalName,
                                               TSigArg signalArg,
                                               SignalWorkflowConfiguration signalConfig = null,
                                               CancellationToken cancelToken = default)
        {
            await _temporalClient.EnsureConnectedAsync(cancelToken);

            (SemaphoreSlim bindingLock, string workflowChainId) = IsBound
                                                                    ? (null, WorkflowChainId)
                                                                    : await BeginBindingOperationIfRequiredAsync(cancelToken);
            try
            {
                SignalWorkflow.Arguments<TSigArg> opArgs = new(Namespace,
                                                               WorkflowId,
                                                               workflowChainId,
                                                               WorkflowRunId: null,
                                                               signalName,
                                                               signalArg,
                                                               signalConfig ?? SignalWorkflowConfiguration.Default,
                                                               cancelToken);

                ITemporalClientInterceptor invokerPipeline = GetOrCreateServiceInvocationPipeline(opArgs);
                SignalWorkflow.Result resSigWf = await invokerPipeline.SignalWorkflowAsync(opArgs);
                TryApplyBindingIfLockIsHeld(bindingLock, resSigWf);
            }
            finally
            {
                bindingLock?.Release();
            }
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowHandle.QueryAsync{TResult}(String, CancellationToken)"/>)
        /// for a detailed description.
        /// </summary>
        public Task<TResult> QueryAsync<TResult>(string queryName,
                                                 QueryWorkflowConfiguration queryConfig = null,
                                                 CancellationToken cancelToken = default)
        {
            return QueryAsync<IPayload.Void, TResult>(queryName, Payload.Void, queryConfig, cancelToken);
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowHandle.QueryAsync{TQryArg, TResult}(String, TQryArg, CancellationToken)"/>)
        /// for a detailed description.
        /// </summary>
        public async Task<TResult> QueryAsync<TQryArg, TResult>(string queryName,
                                                                TQryArg queryArg,
                                                                QueryWorkflowConfiguration queryConfig = null,
                                                                CancellationToken cancelToken = default)
        {
            await _temporalClient.EnsureConnectedAsync(cancelToken);

            (SemaphoreSlim bindingLock, string workflowChainId) = IsBound
                                                                    ? (null, WorkflowChainId)
                                                                    : await BeginBindingOperationIfRequiredAsync(cancelToken);
            try
            {
                QueryWorkflow.Arguments<TQryArg> opArgs = new(Namespace,
                                                              WorkflowId,
                                                              workflowChainId,
                                                              WorkflowRunId: null,
                                                              queryName,
                                                              queryArg,
                                                              queryConfig ?? QueryWorkflowConfiguration.Default,
                                                              cancelToken);

                ITemporalClientInterceptor invokerPipeline = GetOrCreateServiceInvocationPipeline(opArgs);
                QueryWorkflow.Result<TResult> resQryWf = await invokerPipeline.QueryWorkflowAsync<TQryArg, TResult>(opArgs);
                TryApplyBindingIfLockIsHeld(bindingLock, resQryWf);
                return resQryWf.Value;
            }
            finally
            {
                bindingLock?.Release();
            }
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowHandle.RequestCancellationAsync(CancellationToken)"/>)
        /// for a detailed description.
        /// </summary>
        public async Task RequestCancellationAsync(CancellationToken cancelToken = default)
        {
            await _temporalClient.EnsureConnectedAsync(cancelToken);

            (SemaphoreSlim bindingLock, string workflowChainId) = IsBound
                                                                    ? (null, WorkflowChainId)
                                                                    : await BeginBindingOperationIfRequiredAsync(cancelToken);
            try
            {
                RequestCancellation.Arguments opArgs = new(Namespace,
                                                           WorkflowId,
                                                           workflowChainId,
                                                           WorkflowRunId: null,
                                                           cancelToken);

                ITemporalClientInterceptor invokerPipeline = GetOrCreateServiceInvocationPipeline(opArgs);
                RequestCancellation.Result resReqCnclWf = await invokerPipeline.RequestCancellationAsync(opArgs);
                TryApplyBindingIfLockIsHeld(bindingLock, resReqCnclWf);
            }
            finally
            {
                bindingLock?.Release();
            }
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowHandle.TerminateAsync(String, CancellationToken)"/>) for a detailed description.
        /// </summary>
        public Task TerminateAsync(string reason = null,
                                   CancellationToken cancelToken = default)
        {
            return TerminateAsync(reason, Payload.Void, cancelToken);
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowHandle.TerminateAsync{TTermArg}(String, TTermArg, CancellationToken)"/>)
        /// for a detailed description.
        /// </summary>
        public async Task TerminateAsync<TTermArg>(string reason,
                                                   TTermArg details,
                                                   CancellationToken cancelToken = default)
        {
            await _temporalClient.EnsureConnectedAsync(cancelToken);

            (SemaphoreSlim bindingLock, string workflowChainId) = IsBound
                                                                    ? (null, WorkflowChainId)
                                                                    : await BeginBindingOperationIfRequiredAsync(cancelToken);
            try
            {
                TerminateWorkflow.Arguments<TTermArg> opArgs = new(Namespace,
                                                                   WorkflowId,
                                                                   workflowChainId,
                                                                   WorkflowRunId: null,
                                                                   reason,
                                                                   details,
                                                                   cancelToken);

                ITemporalClientInterceptor invokerPipeline = GetOrCreateServiceInvocationPipeline(opArgs);
                TerminateWorkflow.Result resTermWf = await invokerPipeline.TerminateWorkflowAsync(opArgs);
                TryApplyBindingIfLockIsHeld(bindingLock, resTermWf);
            }
            finally
            {
                bindingLock?.Release();
            }
        }

        #endregion --- APIs to interact with the chain ---


        #region -- Internals --

        internal ITemporalClientInterceptor GetServiceInvocationPipeline()
        {
            ValidateIsBound();
            return _serviceInvocationPipeline;
        }

        #endregion -- Internals --


        #region -- Privates --

        private void ValidateIsBound()
        {
            if (IsBound)
            {
                return;
            }

            throw new InvalidOperationException($"Cannot perform this operation because this {nameof(IWorkflowHandle)} instance"
                                              + $" is not bound to a particular workflow chain. An \"unbound\""
                                              + $" {nameof(IWorkflowHandle)} represents the last (aka most recent) workflow chain"
                                              + $" with the given {nameof(WorkflowId)}. An {nameof(IWorkflowHandle)} gets bound"
                                              + $" when any API is invoked that requires an interaction with an actual workflow"
                                              + $" run on the server. At that time, the most recent workflow run (and, therefore,"
                                              + $" the workflow chain that contains it) is determined, and the"
                                              + $" {nameof(IWorkflowHandle)} instance is bound to that chain. From then on the"
                                              + $" {nameof(IWorkflowHandle)} instance always refers to that particular workflow"
                                              + $" chain, even if more recent chains are started later. To bind this instance,"
                                              + $" call `{nameof(EnsureBoundAsync)}(..)` or any other API that interacts with a"
                                              + $" particular workflow.");
        }

        private void ValidateIsNotBound()
        {
            if (!IsBound)
            {
                return;
            }

            throw new InvalidOperationException($"Cannot perform this operation because this {nameof(IWorkflowHandle)} instance"
                                              + $" is already bound to a particular workflow chain. A \"bound\" {nameof(IWorkflowHandle)}"
                                              + $" instance represents a particular workflow chain with a particular {nameof(WorkflowId)}"
                                              + $" and a particular {nameof(WorkflowChainId)} (aka the {nameof(IWorkflowRunHandle.WorkflowRunId)}"
                                              + $" of the first workflow run in the chain). On contrary, an \"unbound\""
                                              + $" {nameof(IWorkflowHandle)} represents the last (aka most recent) workflow chain"
                                              + $" with of all chains with a particular {nameof(WorkflowId)}.");
        }

        private void Bind(string workflowChainId)
        {
            Validate.NotNullOrWhitespace(workflowChainId);
            ValidateIsNotBound();
            _workflowChainId = workflowChainId;
            _isBound = true;
        }

        /// <summary>
        /// Binds this chain to the chain-id provided by <c>bindingResult</c> if it can provide the bound chain info.
        /// Otherwise, does nothing.
        /// Returns whether the binding was applied during this invocation.
        /// </summary>        
        private bool TryApplyBindingIfLockIsHeld(SemaphoreSlim bindingLock, IWorkflowOperationResult bindingOperationResult)
        {
            if (bindingLock != null
                    && bindingOperationResult != null
                    && bindingOperationResult.TryGetBoundWorkflowChainId(out string boundChainId))
            {
                Bind(boundChainId);
                return true;
            }

            return false;
        }

        private async Task<(SemaphoreSlim BindingLock, string WorkflowChainId)> BeginBindingOperationIfRequiredAsync(CancellationToken cancelToken)
        {
            if (IsBound)
            {
                return new(null, WorkflowChainId);
            }

            SemaphoreSlim bindingLock = GetOrCreateBindingLock();

            await bindingLock.WaitAsync(cancelToken);

            if (IsBound)
            {
                bindingLock.Release();
                return new(null, WorkflowChainId);
            }

            return new(bindingLock, null);
        }

        private SemaphoreSlim GetOrCreateBindingLock()
        {
            SemaphoreSlim bindingLock = Volatile.Read(ref _bindigLock);

            if (bindingLock == null)
            {
                SemaphoreSlim newLock = new(1);
                bindingLock = Concurrent.TrySetOrGetValue(ref _bindigLock, newLock, out bool isNewLockStored);

                if (!isNewLockStored)
                {
                    newLock.Dispose();
                }
            }

            return bindingLock;
        }

        private ITemporalClientInterceptor GetOrCreateServiceInvocationPipeline(IWorkflowOperationArguments opArgs)
        {
            // This method (`GetOrCreateServiceInvocationPipeline`) is only called either DURING the first "binding"
            // operation or AFTER the first "binding" operation. Either way the `_bindigLock` is already initialized
            // (i.e., not null). Regardless of the actual semaphone state, we can use that obect to take a local
            // `pipelineCreationLock` to protect pipeline contruction.

            return _temporalClient.GetOrCreateServiceInvocationPipeline(this, ref _serviceInvocationPipeline, _bindigLock, opArgs);
        }

        #endregion -- Privates --


        #region -- Dispose --

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                SemaphoreSlim bindingLock = Interlocked.Exchange(ref _bindigLock, null);
                if (bindingLock != null)
                {
                    bindingLock.Dispose();
                }

                ITemporalClientInterceptor pipeline = Interlocked.Exchange(ref _serviceInvocationPipeline, null);
                if (pipeline != null)
                {
                    pipeline.Dispose();
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

        #endregion -- Dispose --
    }
}
