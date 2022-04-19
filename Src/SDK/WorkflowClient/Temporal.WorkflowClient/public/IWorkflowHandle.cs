using System;
using System.Threading;
using System.Threading.Tasks;
using Temporal.Api.Enums.V1;
using Temporal.Api.WorkflowService.V1;
using Temporal.Common;
using Temporal.WorkflowClient.Interceptors;
using Temporal.WorkflowClient.OperationConfigurations;

namespace Temporal.WorkflowClient
{
    public interface IWorkflowHandle : IDisposable
    {
        string Namespace { get; }
        string WorkflowId { get; }
        bool IsBound { get; }

        /// <summary>
        /// Id of the first run in the chain..
        /// Throws invalid operation is not bound.
        /// </summary>
        string WorkflowChainId { get; }

        Task<string> GetWorkflowTypeNameAsync(CancellationToken cancelToken = default);

        Task<WorkflowExecutionStatus> GetStatusAsync(CancellationToken cancelToken = default);

        Task<bool> ExistsAsync(CancellationToken cancelToken = default);

        Task<DescribeWorkflowExecutionResponse> DescribeAsync(CancellationToken cancelToken = default);

        /// <summary>
        /// <para>Ensure that this <c>IWorkflowHandle</c> instance is "bound" to a particular workflow chain on the server.</para>       
        /// <para>If this <c>IWorkflowHandle</c> instance is already bound - this API completes immediately
        /// (before even checking cancelToken).<br/>
        /// Otherwise this API will comminucate with the Temporal server to determine the currently latest (aka most recent) workflow
        /// run with the with the <c>WorkflowId</c> associated with this <c>IWorkflowHandle</c> instance (and, therefore, the workflow
        /// chain that contains that run). Then, this <c>IWorkflowHandle</c> instance is bound to that chain. From then on, the
        /// <c>IWorkflowHandle</c> instance always refers to that particular workflow chain, even if more recent chains are started
        /// later.</para>
        /// (Design note: If not bound AND the WorkflowChainBindingPolicy REQUIRES starting new chain - fail telling to use the other overload.)
        /// </summary>
        /// <remarks>
        /// <strong><em>Bound</em> and <em>unbound</em> <c>IWorkflowHandle</c> instances:</strong>
        /// An "unbound" <c>IWorkflowHandle</c> represents the last (aka most recent) workflow chain with the given <c>WorkflowId</c>.
        /// An <c>IWorkflowHandle</c> gets bound when any API is invoked that requires an interaction with an actual workflow
        /// run on the server. At that time, the most recent workflow run (and, therefore, the workflow chain that contains it)
        /// is determined, and the <c>IWorkflowHandle</c> instance is bound to that chain. From then on the <c>IWorkflowHandle</c>
        /// instance always refers to that particular workflow chain, even if more recent chains are started later.
        /// This mechanism ensures that a given <c>IWorkflowHandle</c> instance is not mistakenly used to interact with different 
        /// workflow chains, and thus with different logical workflows on the server.
        /// </remarks>
        Task EnsureBoundAsync(CancellationToken cancelToken);

        #region StartAsync(..)
        /// <summary>If already bound - fail. Otherwise, start and bind to result.</summary>        
        Task<StartWorkflow.Result> StartAsync<TWfArg>(string workflowTypeName,
                                                      string taskQueue,
                                                      TWfArg workflowArg,
                                                      StartWorkflowChainConfiguration workflowConfig = null,
                                                      bool throwIfWorkflowChainAlreadyExists = true,
                                                      CancellationToken cancelToken = default);
        #endregion StartAsync(..)

        #region StartWithSignalAsync(..)
        /// <summary>If already bound - fail. Otherwise, start and bind to result.</summary>        
        Task<StartWorkflow.Result> StartWithSignalAsync<TWfArg, TSigArg>(string workflowTypeName,
                                                                         string taskQueue,
                                                                         TWfArg workflowArg,
                                                                         string signalName,
                                                                         TSigArg signalArg,
                                                                         StartWorkflowChainConfiguration workflowConfig = null,
                                                                         CancellationToken cancelToken = default);
        #endregion StartWithSignalAsync(..)

        #region --- GetXxxRunAsync(..) APIs to access a specific run ---

        /// <summary>
        /// Gets a handle for a workflow run, within thew workflow chain represented by this handle.
        /// The created run handle uses the WorkflowId of this chain.
        /// This method does not validate whehter the run with the specified <c>workflowRunId</c> actually exists
        /// within the chain represented by this handle.
        /// </summary>
        /// <exception cref="InvalidOperationException">If this handle is not bound.</exception>
        IWorkflowRunHandle CreateRunHandle(string workflowRunId);

        /// <summary>Get the first / initial run in this chain.</summary>
        Task<IWorkflowRunHandle> GetFirstRunAsync(CancellationToken cancelToken = default);

        /// <summary>Get the most recent run in this chain.</summary>
        Task<IWorkflowRunHandle> GetLatestRunAsync(CancellationToken cancelToken = default);

        /// <summary>
        /// Get the very last run IF it is already known to be final (no further runs can/will follow).<br />
        /// If it is not yet known whether the latest run is final, this API will not fail, but it will return False.
        /// There is no long poll. This can be used to get result of the chain IF chain has finished (grab result of final run).</summary>
        /// </summary>
        Task<TryResult<IWorkflowRunHandle>> TryGetFinalRunAsync(CancellationToken cancelToken = default);

        #endregion --- GetXxxRunAsync(..) APIs to access a specific run ---

        // <summary>Lists runs of this chain only. Needs overloads with filters? Not in V-Alpha.</summary>
        //Task<IPaginatedReadOnlyCollectionPage<IWorkflowRunHandle>> ListRunsAsync(NeedsDesign oneOrMoreArgs);

        #region --- APIs to interact with the chain ---

        // Invoking these APIs will interact with the currently active (aka latest, aka running) Run in this chain.
        // In all common scenarios this is what you want.
        // In some rare scenarios when you need to interact with a specific Run, obrain the corresponding IWorkflowRunHandle instance
        // and invoke the corresponding API on that instance.

        /// <summary>The returned task completes when this chain finishes (incl. any runs not yet started). Performs long poll.</summary>
        Task<TResult> GetResultAsync<TResult>(CancellationToken cancelToken = default);

        Task<IWorkflowRunResult> AwaitConclusionAsync(CancellationToken cancelToken = default);

        Task SignalAsync(string signalName,
                         CancellationToken cancelToken = default);

        Task SignalAsync<TSigArg>(string signalName,
                                  TSigArg signalArg,
                                  SignalWorkflowConfiguration signalConfig = null,
                                  CancellationToken cancelToken = default);

        Task<TResult> QueryAsync<TResult>(string queryName,
                                          CancellationToken cancelToken = default);

        Task<TResult> QueryAsync<TQryArg, TResult>(string queryName,
                                                   TQryArg queryArg,
                                                   CancellationToken cancelToken = default);

        Task RequestCancellationAsync(CancellationToken cancelToken = default);

        Task TerminateAsync(string reason = null,
                            CancellationToken cancelToken = default);

        Task TerminateAsync<TTermArg>(string reason,
                                      TTermArg details,
                                      CancellationToken cancelToken = default);

        #endregion --- APIs to interact with the chain ---
    }
}
