using System;
using System.Threading;
using System.Threading.Tasks;
using Candidly.Util;
using Temporal.Api.Enums.V1;
using Temporal.Api.WorkflowService.V1;
using Temporal.Common;
using Temporal.WorkflowClient.Interceptors;

namespace Temporal.WorkflowClient
{
    internal class WorkflowChain : IWorkflowChain, IDisposable
    {
        public static void ValidateWorkflowChainId(string workflowChainId)
        {
            if (workflowChainId != null && String.IsNullOrWhiteSpace(workflowChainId))
            {
                throw new ArgumentException($"{nameof(workflowChainId)} must be either null, or a non-empty-or-whitespace string."
                                          + $" However, \"{workflowChainId}\" was specified.");
            }
        }

        private readonly TemporalClient _temporalClient;

        private SemaphoreSlim _bindigLock = null;
        private string _workflowChainId;
        private bool _isBound;

        private ITemporalClientInterceptor _serviceInvocationPipeline = null;

        internal WorkflowChain(TemporalClient temporalClient, string workflowId)
            : this(temporalClient, workflowId, workflowChainId: null)
        {
        }

        internal WorkflowChain(TemporalClient temporalClient, string workflowId, string workflowChainId)
        {
            Validate.NotNull(temporalClient);
            Validate.NotNullOrWhitespace(temporalClient.Namespace);
            Validate.NotNullOrWhitespace(workflowId);
            ValidateWorkflowChainId(workflowChainId);

            _temporalClient = temporalClient;

            WorkflowId = workflowId;

            _workflowChainId = workflowChainId;
            _isBound = (workflowChainId != null);
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.Namespace"/>) for a detailed description.
        /// </summary>
        public string Namespace
        {
            get { return _temporalClient.Namespace; }
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.WorkflowId"/>) for a detailed description.
        /// </summary>
        public string WorkflowId { get; }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.IsBound"/>) for a detailed description.
        /// </summary>
        public bool IsBound { get { return Volatile.Read(ref _isBound); } }

        /// <summary>
        /// The <c>WorkflowChainId</c> is the <c>WorkflowRunId</c> of the first run in the workflow chain.<br />
        /// Calling the getter for this property will result in <c>InvalidOperationException</c> if this <c>WorkflowChain</c> is not bound.
        /// </summary>
        /// <remarks>See the implemented iface API (<see cref="IWorkflowChain.WorkflowChainId"/>) for a detailed description.</remarks>
        public string WorkflowChainId
        {
            get
            {
                ValidateIsBound();
                return Volatile.Read(ref _workflowChainId);
            }
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.GetWorkflowTypeNameAsync(CancellationToken)"/>) for a detailed description.
        /// </summary>
        public async Task<string> GetWorkflowTypeNameAsync(CancellationToken cancelToken)
        {
            DescribeWorkflowExecutionResponse resDescrWf = await DescribeAsync(cancelToken);
            return resDescrWf.WorkflowExecutionInfo.Type.Name;
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.GetStatusAsync(CancellationToken)"/>) for a detailed description.
        /// </summary>
        public async Task<WorkflowExecutionStatus> GetStatusAsync(CancellationToken cancelToken)
        {
            DescribeWorkflowExecutionResponse resDescrWf = await DescribeAsync(cancelToken);
            return resDescrWf.WorkflowExecutionInfo.Status;
        }

        /// <summary>
        /// Should this be called TryDescribeAsync?<br/>
        /// See the implemented iface API (<see cref="IWorkflowChain.CheckExistsAsync(CancellationToken)"/>) for a detailed description.
        /// </summary>        
        public Task<TryResult<DescribeWorkflowExecutionResponse>> CheckExistsAsync(CancellationToken cancelToken)
        {
            return TryDescribeAsync(throwNotExists: false, cancelToken);
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.DescribeAsync(CancellationToken)"/>) for a detailed description.
        /// </summary>
        public async Task<DescribeWorkflowExecutionResponse> DescribeAsync(CancellationToken cancelToken)
        {
            return (await TryDescribeAsync(throwNotExists: true, cancelToken)).Result;
        }

        private Task<TryResult<DescribeWorkflowExecutionResponse>> TryDescribeAsync(bool throwNotExists, CancellationToken cancelToken)
        {
            throw new NotImplementedException("@ToDo");
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.EnsureBoundAsync(CancellationToken)"/>) for a
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

                ITemporalClientInterceptor invokerPipeline = GetOrCreateServiceInvocationPipeline();
                string chainId = await invokerPipeline.GetLatestWorkflowChainId(Namespace, WorkflowId, cancelToken);

                ValidateWorkflowChainId(chainId);
                if (chainId != null)
                {
                    Bind(chainId);
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
        /// <see cref="IWorkflowChain.StartAsync{TWfArg}(String, String, TWfArg, StartWorkflowChainConfiguration, CancellationToken)"/>
        /// ) for a detailed description.
        /// </summary>
        public Task StartAsync<TWfArg>(string workflowTypeName,
                                       string taskQueue,
                                       TWfArg workflowArg,
                                       StartWorkflowChainConfiguration workflowConfig = null,
                                       CancellationToken cancelToken = default)
        {
            return StartAsync(workflowTypeName,
                              taskQueue,
                              workflowArg,
                              failIfWorkflowChainAlreadyExists: true,
                              workflowConfig,
                              cancelToken);
        }

        public async Task<StartWorkflowResult> StartAsync<TWfArg>(string workflowTypeName,
                                                                  string taskQueue,
                                                                  TWfArg workflowArg,
                                                                  bool failIfWorkflowChainAlreadyExists,
                                                                  StartWorkflowChainConfiguration workflowConfig = null,
                                                                  CancellationToken cancelToken = default)
        {
            ValidateIsNotBound();
            await _temporalClient.EnsureConnectedAsync(cancelToken);

            SemaphoreSlim bindingLock = GetOrCreateBindingLock();
            await bindingLock.WaitAsync(cancelToken);
            try
            {
                ValidateIsNotBound();

                workflowConfig = workflowConfig ?? StartWorkflowChainConfiguration.Default;

                ITemporalClientInterceptor invokerPipeline = GetOrCreateServiceInvocationPipeline();
                StartWorkflowResult resStartWf = await invokerPipeline.StartWorkflowAsync(Namespace,
                                                                                          WorkflowId,
                                                                                          workflowTypeName,
                                                                                          taskQueue,
                                                                                          workflowArg,
                                                                                          workflowConfig,
                                                                                          failIfWorkflowChainAlreadyExists,
                                                                                          cancelToken);
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

        #region StartWithSignalAsync(..)

        /// <summary>
        /// See the implemented iface API (
        /// <see cref="IWorkflowChain.StartWithSignalAsync{TWfArg, TSigArg}(String, String, TWfArg, String, TSigArg, StartWorkflowChainConfiguration, CancellationToken)"/>
        /// ) for a detailed description.
        /// </summary>
        public Task StartWithSignalAsync<TWfArg, TSigArg>(string workflowTypeName,
                                                          string taskQueue,
                                                          TWfArg workflowArg,
                                                          string signalName,
                                                          TSigArg signalArg,
                                                          StartWorkflowChainConfiguration workflowConfig = null,
                                                          CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        #endregion StartWithSignalAsync(..)

        #region StartIfNotRunningAsync(..)

        /// <summary>
        /// See the implemented iface API (
        /// <see cref="IWorkflowChain.StartIfNotRunningAsync(String, String, StartWorkflowChainConfiguration, CancellationToken)"/>
        /// ) for a detailed description.
        /// </summary>
        public Task<bool> StartIfNotRunningAsync(string workflowTypeName,
                                                 string taskQueue,
                                                 StartWorkflowChainConfiguration workflowConfig = null,
                                                 CancellationToken cancelToken = default)
        {
            return StartIfNotRunningAsync(workflowTypeName, taskQueue, Payload.Void, workflowConfig, cancelToken);
        }



        /// <summary>
        /// See the implemented iface API (
        /// <see cref="IWorkflowChain.StartIfNotRunningAsync{TWfArg}(String, String, TWfArg, StartWorkflowChainConfiguration, CancellationToken)"/>
        /// ) for a detailed description.
        /// </summary>
        public async Task<bool> StartIfNotRunningAsync<TWfArg>(string workflowTypeName,
                                                         string taskQueue,
                                                         TWfArg workflowArg,
                                                         StartWorkflowChainConfiguration workflowConfig = null,
                                                         CancellationToken cancelToken = default)
        {
            StartWorkflowResult resStartWf = await StartAsync(workflowTypeName,
                                                              taskQueue,
                                                              workflowArg,
                                                              failIfWorkflowChainAlreadyExists: false,
                                                              workflowConfig,
                                                              cancelToken);

            return (resStartWf.Code == StartWorkflowResult.Status.OK);
        }
        #endregion StartIfNotRunningAsync(..)

        #region --- GetXxxRunAsync(..) APIs to access a specific run ---

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.Namespace"/>) for a detailed description.
        /// </summary>
        public Task<TryResult<IWorkflowRun>> TryGetRunAsync(string workflowRunId,
                                                            CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.Namespace"/>) for a detailed description.
        /// </summary>
        public Task<IWorkflowRun> GetFirstRunAsync(CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.Namespace"/>) for a detailed description.
        /// </summary>
        public Task<IWorkflowRun> GetLatestRunAsync(CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.Namespace"/>) for a detailed description.
        /// </summary>
        public Task<TryResult<IWorkflowRun>> TryGetFinalRunAsync(CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        #endregion --- GetXxxRunAsync(..) APIs to access a specific run ---

        // <summary>Lists runs of this chain only. Needs overloads with filters? Not in V-Alpha.</summary>
        // @ToDo
        //Task<IPaginatedReadOnlyCollectionPage<IWorkflowRun>> ListRunsAsync(NeedsDesign oneOrMoreArgs);

        #region --- APIs to interact with the chain ---

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.GetResultAsync{TResult}(CancellationToken)"/>)
        /// for a detailed description.
        /// </summary>
        public async Task<TResult> GetResultAsync<TResult>(CancellationToken cancelToken = default)
        {
            IWorkflowRunResult finalRunResult = await AwaitConclusionAsync(cancelToken);
            return finalRunResult.GetValue<TResult>();
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.AwaitConclusionAync(CancellationToken)"/>) for a detailed description.
        /// </summary>
        public async Task<IWorkflowRunResult> AwaitConclusionAsync(CancellationToken cancelToken)
        {
            await _temporalClient.EnsureConnectedAsync(cancelToken);
            await ForceBindHackAsync(cancelToken);

            (SemaphoreSlim bindingLock, string workflowChainId) = IsBound
                                                                    ? (null, WorkflowChainId)
                                                                    : await BeginBindingOperationIfRequiredAsync(cancelToken);
            try
            {
                ITemporalClientInterceptor invokerPipeline = GetOrCreateServiceInvocationPipeline();
                IWorkflowRunResult resWfRun = await invokerPipeline.AwaitConclusionAsync(Namespace,
                                                                                         WorkflowId,
                                                                                         workflowChainId,
                                                                                         workflowRunId: null,
                                                                                         followChain: true,
                                                                                         cancelToken);

                if (resWfRun != null && resWfRun is WorkflowRunResult resWfRunIntrnlImpl)
                {
                    resWfRunIntrnlImpl.TemporalClient = _temporalClient;
                }

                ApplyBindingIfOperationSucceeded(bindingLock, resWfRun);
                return resWfRun;
            }
            finally
            {
                bindingLock?.Release();
            }
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.SignalAsync(String, CancellationToken)"/>) for a detailed description.
        /// </summary>
        public Task SignalAsync(string signalName,
                                CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.SignalAsync{TSigArg}(String, TSigArg, CancellationToken)"/>)
        /// for a detailed description.
        /// </summary>
        public Task SignalAsync<TSigArg>(string signalName,
                                         TSigArg signalArg,
                                         CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.QueryAsync{TResult}(String, CancellationToken)"/>)
        /// for a detailed description.
        /// </summary>
        public Task<TResult> QueryAsync<TResult>(string queryName,
                                                 CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.QueryAsync{TQryArg, TResult}(String, TQryArg, CancellationToken)"/>)
        /// for a detailed description.
        /// </summary>
        public Task<TResult> QueryAsync<TQryArg, TResult>(string queryName,
                                                          TQryArg queryArg,
                                                          CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.RequestCancellationAsync(CancellationToken)"/>)
        /// for a detailed description.
        /// </summary>
        public Task RequestCancellationAsync(CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.TerminateAsync(String, CancellationToken)"/>) for a detailed description.
        /// </summary>
        public Task TerminateAsync(string reason = null,
                                   CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.TerminateAsync{TTermArg}(String, TTermArg, CancellationToken)"/>)
        /// for a detailed description.
        /// </summary>
        public Task TerminateAsync<TTermArg>(string reason,
                                             TTermArg details,
                                             CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        #endregion --- APIs to interact with the chain ---

        #region Privates

        /// <summary>
        /// Many server APIs optonally take a null workflow-run-id to refer to the latest run/chain for the given workflow-run-id.
        /// In the long-term, we will make such APIs return the workflow-chain-id that was chosen (aka the first-run-of-the-chain-id).
        /// Once that is done, and we will bind this chain to that ID.
        /// ! At that time we must remove this method and all calls to it !
        /// Until then we use this method to ensure in the same observable behaviour art the cost of one additional remote call
        /// before the first remote call the chain makes.
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        private Task ForceBindHackAsync(CancellationToken cancelToken)
        {
            return EnsureBoundAsync(cancelToken);
        }

        private ITemporalClientInterceptor GetOrCreateServiceInvocationPipeline()
        {

            ITemporalClientInterceptor pipeline = Volatile.Read(ref _serviceInvocationPipeline);

            if (pipeline == null)
            {
                ITemporalClientInterceptor newPipeline = _temporalClient.CreateServiceInvocationPipeline(this);
                pipeline = Concurrent.TrySetOrGetValue(ref _serviceInvocationPipeline, newPipeline);
            }

            return pipeline;
        }

        private void Bind(string workflowChainId)
        {
            Validate.NotNullOrWhitespace(workflowChainId);
            _workflowChainId = workflowChainId;
            _isBound = true;
        }

        private bool ApplyBindingIfOperationSucceeded(SemaphoreSlim bindingLock, IWorkflowChainBindingResult bindingResult)
        {
            if (bindingLock != null && bindingResult != null && bindingResult.TryGetBoundWorkflowChainId(out string boundChainId))
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

            // Future: Should we dispose of the `_bindigLock` when we are sure that we no longer need it?

            return bindingLock;
        }

        private void ValidateIsNotBound()
        {
            if (!IsBound)
            {
                return;
            }

            throw new InvalidOperationException($"Cannot perform this operation becasue this {nameof(IWorkflowChain)} instance"
                                              + $" is already bound to a particular workflow chain. A \"bound\" {nameof(IWorkflowChain)}"
                                              + $" instance represents a particular workflow chain with a particular {nameof(WorkflowId)}"
                                              + $" and a particular {nameof(WorkflowChainId)} (aka the {nameof(IWorkflowRun.WorkflowRunId)}"
                                              + $" of the first workflow run in the chain). On contrary, an \"unbound\""
                                              + $" {nameof(IWorkflowChain)} represents the last (aka most recent) workflow chain"
                                              + $" with of all chains with a particular {nameof(WorkflowId)}.");
        }

        private void ValidateIsBound()
        {
            if (IsBound)
            {
                return;
            }

            throw new InvalidOperationException($"Cannot perform this operation becasue this {nameof(IWorkflowChain)} instance"
                                              + $" is not bound to a particular workflow chain. An \"unbound\""
                                              + $" {nameof(IWorkflowChain)} represents the last (aka most recent) workflow chain"
                                              + $" with the given {nameof(WorkflowId)}. An {nameof(IWorkflowChain)} gets bound"
                                              + $" when any API is invoked that requires an interaction with an actual workflow"
                                              + $" run on the server. At that time, the most recent workflow run (and, therefore,"
                                              + $" the workflow chain that contains it) is determined, and the"
                                              + $" {nameof(IWorkflowChain)} instance is bound to that chain. From then on the"
                                              + $" {nameof(IWorkflowChain)} instance always refers to that particular workflow"
                                              + $" chain, even if more recent chains are started later. To bind this instance,"
                                              + $" call `{nameof(EnsureBoundAsync)}(..)` or any other API that interacts with a"
                                              + $" particular workflow.");
        }

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
        // ~WorkflowChain()
        // {
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion Privates
    }
}
