using System;
using System.Threading;
using System.Threading.Tasks;
using Candidly.Util;
using Temporal.Api.Enums.V1;
using Temporal.Api.WorkflowService.V1;
using Temporal.Common;

namespace Temporal.WorkflowClient
{
    internal class WorkflowChain : IWorkflowChain
    {
        private readonly ITemporalClient _serviceClient;
        private string _workflowChainId;

        internal WorkflowChain(ITemporalClient serviceClient, string workflowId)
            : this(serviceClient, workflowId, workflowChainId: null)
        {
        }

        internal WorkflowChain(ITemporalClient serviceClient, string workflowId, string workflowChainId)
        {
            Validate.NotNull(serviceClient);
            Validate.NotNullOrWhitespace(serviceClient.Namespace);
            Validate.NotNullOrWhitespace(workflowId);

            if (workflowChainId != null && String.IsNullOrWhiteSpace(workflowChainId))
            {
                throw new ArgumentException($"{nameof(workflowChainId)} must be either null, or a non-empty-or-whitespace string."
                                          + $" However, \"{workflowChainId}\" was specified.");
            }

            _serviceClient = serviceClient;
            WorkflowId = workflowId;
            _workflowChainId = workflowChainId;
            IsBound = (workflowChainId != null);
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.Namespace"/>) for a detailed description.
        /// </summary>
        public string Namespace
        {
            get { return _serviceClient.Namespace; }
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.WorkflowId"/>) for a detailed description.
        /// </summary>
        public string WorkflowId { get; }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.IsBound"/>) for a detailed description.
        /// </summary>
        public bool IsBound { get; }

        /// <summary>
        /// The <c>WorkflowChainId</c> is the <c>WorkflowRunId</c> of the first run in the workflow chain.<br />
        /// Calling the getter for this property will result in <c>InvalidOperationException</c> if this <c>WorkflowChain</c> is not bound.
        /// </summary>
        /// <remarks>See the implemented iface API (<see cref="IWorkflowChain.WorkflowChainId"/>) for a detailed description.</remarks>
        public string WorkflowChainId
        {
            get
            {
                if (!IsBound)
                {
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

                return _workflowChainId;
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
        /// (Design note: If not bound && the WorkflowChainBindingPolicy REQUIRES starting new chain - fail telling to use the other overload.)
        /// </summary>        
        public Task EnsureBoundAsync(CancellationToken cancelToken)
        {
            if (IsBound)
            {
                return Task.CompletedTask;
            }

            return DescribeAsync(cancelToken);
        }

        #region StartAsync(..)

        /// <summary>
        /// See the implemented iface API (
        /// <see cref="IWorkflowChain.StartAsync{TWfArg}(String, String, TWfArg, StartWorkflowChainConfiguration, CancellationToken)"/>
        /// ) for a detailed description.
        /// </summary>
        public Task StartAsync<TWfArg>(string workflowTypeName,
                                       string taskQueue,
                                       TWfArg wokflowArg,
                                       StartWorkflowChainConfiguration workflowConfig = null,
                                       CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
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
                                                          TWfArg wokflowArg,
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
            return StartIfNotRunningAsync(workflowTypeName, taskQueue, DataValue.Void, workflowConfig, cancelToken);
        }



        /// <summary>
        /// See the implemented iface API (
        /// <see cref="IWorkflowChain.StartIfNotRunningAsync{TWfArg}(String, String, TWfArg, StartWorkflowChainConfiguration, CancellationToken)"/>
        /// ) for a detailed description.
        /// </summary>
        public Task<bool> StartIfNotRunningAsync<TWfArg>(string workflowTypeName,
                                                         string taskQueue,
                                                         TWfArg workflowArg,
                                                         StartWorkflowChainConfiguration workflowConfig = null,
                                                         CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
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
        /// See the implemented iface API (<see cref="IWorkflowChain.Namespace"/>) for a detailed description.
        /// </summary>
        public Task GetResultAsync(CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.Namespace"/>) for a detailed description.
        /// </summary>
        public Task<TResult> GetResultAsync<TResult>(CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.Namespace"/>) for a detailed description.
        /// </summary>
        public Task<IWorkflowChainResult> AwaitConclusionAync(CancellationToken cancelToken)
        {
            throw new NotImplementedException("@ToDo");
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.Namespace"/>) for a detailed description.
        /// </summary>
        public Task SignalAsync(string signalName,
                                CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.Namespace"/>) for a detailed description.
        /// </summary>
        public Task SignalAsync<TSigArg>(string signalName,
                                         TSigArg signalArg,
                                         CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.Namespace"/>) for a detailed description.
        /// </summary>
        public Task SignalAsync(string signalName,
                                IDataValue signalArgs,
                                CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.Namespace"/>) for a detailed description.
        /// </summary>
        public Task<TResult> QueryAsync<TResult>(string queryName,
                                                 CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.Namespace"/>) for a detailed description.
        /// </summary>
        public Task<TResult> QueryAsync<TQryArg, TResult>(string queryName,
                                                          TQryArg queryArg,
                                                          CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.Namespace"/>) for a detailed description.
        /// </summary>
        public Task<TResult> QueryAsync<TResult>(string queryName,
                                                 IDataValue queryArgs,
                                                 CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.Namespace"/>) for a detailed description.
        /// </summary>
        public Task RequestCancellationAsync(CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.Namespace"/>) for a detailed description.
        /// </summary>
        public Task TerminateAsync(string reason = null,
                                   CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        /// <summary>
        /// See the implemented iface API (<see cref="IWorkflowChain.Namespace"/>) for a detailed description.
        /// </summary>
        public Task TerminateAsync(string reason,
                                   IDataValue details,
                                   CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        #endregion --- APIs to interact with the chain ---
    }
}
