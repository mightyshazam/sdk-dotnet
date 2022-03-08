using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Temporal.Common.DataModel;
using Temporal.Common.WorkflowConfiguration;

using Temporal.Async;
using Temporal.Collections;
using Temporal.Serialization;

namespace Temporal.WorkflowClient
{
    public class ClientApi
    {
    }

    // *** !Re CancellationTokens: In the actual implementation, most APIs that take a CancellationToken will also have
    // *** some overload(s) that do not and use CancellationToken.None instead.
    // *** For now, we omit most of such overloads for brevity and include only the ones needed for samples.

    #region TemporalServiceClient

    public interface ITemporalServiceClient
    {
        #region -- Namespace settings for the client --
        // A TemporalServiceClient is OPTIONALLY "bound" to a namespace.
        // If the client is NOT bound to a namespace, then only APIs that do not require a namespace can be invoked.
        // Other APIs will result in an `InvalidOperationException`.
        // Q: Shall we always bound by default to some namespace with a default name? What would that be?
        // If the client IS bound to a namespace, but that namespace does not exist on the server or the user
        // has no appropriate permissions, then a `NeedsDesignException` will be thrown from the APIs that require the namespace.
        // There are two ways how a client may be bound to a namespace:
        // 1) (Optionally) specify the namespace in the TemporalServiceClientConfiguration passed to the ctor.
        //    In that case the namespace cannot be validated immediately. The client will be bound to the namespace, and invoking
        //    APIs that require the namespace will throw as described above if there is something wrong with the namespace.
        // 2) Call TrySetNamespaceAsync(..).
        //    In that case the API will validate that the namespace exists and can be accessed. If yes, the client will be bound to 
        //    that namespace and the API will return True. Otherwise the previously bound namespace will remain, and the API will
        //    return False. Note that in this scenario, subsequent invocations on APIs that require a namespace may still throw just
        //    like in the 1st case, because the namespace may be altered concurrently on the server by a different client.
        string Namespace { get; }

        Task<bool> TrySetNamespaceAsync(string @namespace);
        Task<bool> TrySetNamespaceAsync(string @namespace, CancellationToken cancelToken);

        // `IsNamespaceAccessibleAsync(..)` returns whether `TrySetNamespaceAsync(..)` would succeed without actually setting the NS.
        Task<bool> IsNamespaceAccessibleAsync(string @namespace, CancellationToken cancelToken);
        #endregion -- Namespace settings for the client --


        #region -- Workflow access and control APIs --

        #region StartNewWorkflowAsync(..)
        // Future: Consider overloads auto-generate a random GUID-based `workflowId`.

        Task<WorkflowConsecution> StartNewWorkflowAsync(string workflowTypeName,
                                                        string workflowId,
                                                        string taskQueue);

        Task<WorkflowConsecution> StartNewWorkflowAsync(string workflowTypeName,
                                                        string workflowId,
                                                        string taskQueue,
                                                        CancellationToken cancelToken);

        Task<WorkflowConsecution> StartNewWorkflowAsync(string workflowTypeName,
                                                        string workflowId,
                                                        string taskQueue,
                                                        IDataValue inputArgs);

        Task<WorkflowConsecution> StartNewWorkflowAsync(string workflowTypeName,
                                                        string workflowId,
                                                        string taskQueue,
                                                        IDataValue inputArgs,
                                                        CancellationToken cancelToken);

        Task<WorkflowConsecution> StartNewWorkflowAsync(string workflowTypeName,
                                                        string workflowId,
                                                        IWorkflowExecutionConfiguration workflowConfig,
                                                        IDataValue inputArgs,
                                                        WorkflowConsecutionClientConfiguration clientConfig,
                                                        CancellationToken cancelToken);
        #endregion StartNewWorkflowAsync(..)

        #region StartNewWorkflowWithSignalAsync(..)
        // Future: Consider overloads auto-generate a random GUID-based `workflowId`.
        Task<WorkflowConsecution> StartNewWorkflowWithSignalAsync(string workflowTypeName,
                                                                 string workflowId,
                                                                 string taskQueue,
                                                                 string signalName,
                                                                 CancellationToken cancelToken);

        Task<WorkflowConsecution> StartNewWorkflowWithSignalAsync(string workflowTypeName,
                                                                  string workflowId,
                                                                  string taskQueue,
                                                                  string signalName,
                                                                  IDataValue signalArgs,
                                                                  CancellationToken cancelToken);

        Task<WorkflowConsecution> StartNewWorkflowWithSignalAsync(string workflowTypeName,
                                                                  string workflowId,
                                                                  string taskQueue,
                                                                  IDataValue wokflowArgs,
                                                                  string signalName,
                                                                  CancellationToken cancelToken);

        Task<WorkflowConsecution> StartNewWorkflowWithSignalAsync(string workflowTypeName,
                                                                  string workflowId,
                                                                  string taskQueue,
                                                                  IDataValue wokflowArgs,
                                                                  string signalName,
                                                                  IDataValue signalArgs,
                                                                  CancellationToken cancelToken);

        Task<WorkflowConsecution> StartNewWorkflowWithSignalAsync(string workflowTypeName,
                                                                  string workflowId,
                                                                  IWorkflowExecutionConfiguration workflowConfig,
                                                                  IDataValue wokflowArgs,
                                                                  string signalName,
                                                                  IDataValue signalArgs,
                                                                  WorkflowConsecutionClientConfiguration clientConfig,
                                                                  CancellationToken cancelToken);
        #endregion StartNewWorkflowWithSignalAsync(..)

        #region GetOrStartWorkflowAsync(..)
        /// <summary>
        /// Get a handle to a RUNNING workflow consecution with the specified <c>workflowId</c>.
        /// If a consecution with the specified <c>workflowId</c> does not exist,
        /// or if it exists, but is not in the RUNNING state, then a new consecuton is started.
        /// Note that the obtained consecution is RUNNING when the server responds to this API.
        /// It may finish running by the time user code interacts with it.
        /// <br /><br />
        /// If the an existing RUNNING workflow consecution with the specified <c>workflowId</c> is obtained,
        /// and the workflow type of the existing consecution does not match the specified <c>workflowTypeName</c>,
        /// then this API will fail with a <c>NeedsDesignException</c>.
        /// </summary>
        Task<WorkflowConsecution> GetOrStartWorkflowAsync(string workflowTypeName,
                                                          string workflowId,
                                                          string taskQueue);

        Task<WorkflowConsecution> GetOrStartWorkflowAsync(string workflowTypeName,
                                                          string workflowId,
                                                          string taskQueue,
                                                          CancellationToken cancelToken);

        Task<WorkflowConsecution> GetOrStartWorkflowAsync(string workflowTypeName,
                                                          string workflowId,
                                                          string taskQueue,
                                                          IDataValue inputArgs,
                                                          CancellationToken cancelToken);

        Task<WorkflowConsecution> GetOrStartWorkflowAsync(string workflowTypeName,
                                                          string workflowId,
                                                          IWorkflowExecutionConfiguration workflowConfig,
                                                          IDataValue inputArgs,
                                                          WorkflowConsecutionClientConfiguration clientConfig,
                                                          CancellationToken cancelToken);
        #endregion GetOrStartWorkflowAsync(..)

        #region GetWorkflowAsync(..)
        /// <summary>
        /// <para>Finds a specific workflow consecution, IF IT EXISTS.</para>
        /// Invokes the overload <see cref="GetWorkflowAsync(String, String, String, WorkflowConsecutionClientConfiguration, CancellationToken)" />
        /// with <c>workflowTypeName</c>=NULL and <c>workflowConsecutionId</c>=NULL.
        /// </summary>
        /// <see cref="GetWorkflowAsync(String, String, String, WorkflowConsecutionClientConfiguration, CancellationToken)" />
        Task<WorkflowConsecution> GetWorkflowAsync(string workflowId);

        Task<WorkflowConsecution> GetWorkflowAsync(string workflowId,
                                                   CancellationToken cancelToken);

        /// <summary>
        /// <para>Finds a specific workflow consecution, IF IT EXISTS.</para>
        /// Invokes the overload <see cref="GetWorkflowAsync(String, String, String, WorkflowConsecutionClientConfiguration, CancellationToken)" />
        /// with <c>workflowTypeName</c>=NULL.
        /// </summary>
        /// <see cref="GetWorkflowAsync(String, String, String, WorkflowConsecutionClientConfiguration, CancellationToken)" />
        Task<WorkflowConsecution> GetWorkflowAsync(string workflowId,
                                                   string workflowConsecutionId,
                                                   CancellationToken cancelToken);

        /// <summary>
        /// <para>
        ///   Finds a specific workflow consecution, IF IT EXISTS.<br />
        ///   The API throws a `NeedsDesignException` if the requested WorkflowConsecution does not exist (or cannot be accessed).
        /// </para>
        /// <para>
        ///   <c>workflowId</c> and <c>workflowConsecutionId</c> may NOT be BOTH null. However, ONE of these values MAY be null.<br/>
        ///   (<c>workflowConsecutionId</c> is the <c>workflowRunId</c> of the first run in the consecution.)
        /// </para>
        /// <para>
        ///   If <c>workflowId</c> and <c>workflowConsecutionId</c> are BOTH NOT NULL:<br />
        ///   Search by tuple (workflowId, workflowConsecutionId).
        ///   If not exists => Not found.
        /// </para>
        /// <para>
        ///   If <c>workflowId</c> is NULL:<br />
        ///   Search by workflowConsecutionId. This may be a long/slow/inefficient scan.
        ///   If not exists => Not found.
        ///   If exists => <c>workflowId</c> of the created <c>WorkflowConsecution</c> instance is set by the found server value.
        /// </para>
        /// <para>
        ///   If <c>workflowConsecutionId</c> is NULL:<br />
        ///   Search by <c>workflowId</c> and select the latest (most recently started) consecution with that <c>workflowId</c>.
        ///   This may be a long/slow/inefficient scan.
        ///   If not exists => Not found.
        ///   If exists => <c>workflowConsecutionId</c> of the created <c>WorkflowConsecution</c> instance is set by the found server value.
        /// </para>
        /// <para>
        ///   The specified <c>workflowTypeName</c> parameter may be null.
        ///   <br/><br/>
        ///   If a workflow consecution IS FOUND for the specified <c>workflowId</c> and <c>workflowConsecutionId</c>, and:
        ///   <br/><br/>
        ///    - the specified <c>workflowTypeName</c> parameter IS NULL,<br/>
        ///      then the <c>WorkflowTypeName</c> of the returned <c>WorkflowConsecution</c> instance is set by the server value.
        ///   <br/><br/>
        ///    - the specified <c>workflowTypeName</c> parameter IS NOT NULL,<br/>
        ///      then the specified <c>workflowTypeName</c> is compared with the value on the server.
        ///      If they DO NOT MATCH, this API will fail with a <c>NeedsDesignException</c>;
        ///      if the DO MATCH, this API will succeed and return an appropriate <c>WorkflowConsecution</c> instance.
        /// </para>
        /// </summary>        
        Task<WorkflowConsecution> GetWorkflowAsync(string workflowTypeName,
                                                   string workflowId,
                                                   string workflowConsecutionId,
                                                   WorkflowConsecutionClientConfiguration clientConfig,
                                                   CancellationToken cancelToken);

        Task<TryGetResult<WorkflowConsecution>> TryGetWorkflowAsync(string workflowId);

        Task<TryGetResult<WorkflowConsecution>> TryGetWorkflowAsync(string workflowTypeName,
                                                                    string workflowId,
                                                                    string workflowConsecutionId,
                                                                    WorkflowConsecutionClientConfiguration clientConfig,
                                                                    CancellationToken cancelToken);
        #endregion GetWorkflowAsync(..)

        #region GetWorkflowRunAsync(..)
        /// <summary>
        /// Finds a specific workflow run, IF IT EXISTS.<br />
        /// The consecution handle containing the found run can be accessed via the `GetOwnerWorkflowAsync(..)` method of the 
        /// returned `WorkflowRun` instance.<br />
        /// See <see cref="GetWorkflowRunAsync(String, String, WorkflowConsecutionClientConfiguration, CancellationToken)" /> for more detais.
        /// </summary>
        Task<WorkflowRun> GetWorkflowRunAsync(string workflowRunId,
                                              CancellationToken cancelToken);

        /// <summary>
        /// Finds a specific workflow run, IF IT EXISTS.<br />
        /// The consecution handle containing the found run can be accessed via the `GetWorkflowConsecutionAsync(..)` method of the 
        /// returned `WorkflowRun` instance.<br />
        /// See <see cref="GetWorkflowRunAsync(String, String, WorkflowConsecutionClientConfiguration, CancellationToken)" /> for more detais.
        /// </summary>
        Task<WorkflowRun> GetWorkflowRunAsync(string workflowId,
                                              string workflowRunId,
                                              CancellationToken cancelToken);

        /// <summary>
        /// <para>
        ///   Finds a specific workflow run, IF IT EXISTS.<br />
        ///   The API throws a `NeedsDesignException` if the requested WorkflowRun does not exist (or cannot be accessed).<br />
        ///   The consecution handle containing the found run can be accessed via the `GetWorkflowConsecutionAsync(..)` method of the 
        ///   returned `WorkflowRun` instance.
        /// </para>
        /// <c>workflowId</c> may be null. In that case the API may require an inefficient/long DB scan. <br />
        /// <c>workflowRunId</c> may not be null.  <br />
        /// If a run with the specified <c>workflowRunId</c> exists, but the <c>workflowId</c> is not null and does not match,
        /// this API will NOT find that run. <br />
        /// </summary>        
        Task<WorkflowRun> GetWorkflowRunAsync(string workflowId,
                                              string workflowRunId,
                                              WorkflowConsecutionClientConfiguration clientConfig,
                                              CancellationToken cancelToken);
        #endregion GetWorkflowRunAsync(..)

        #endregion -- Workflow access and control APIs --

        // ----------- APIs below in this class will not be part of the initial version (V-Alpha). -----------

        #region -- Workflow listing APIs --
        /// <summary>
        /// Lists `WorkflowConsecution`s (not `WorkflowRun`s). Accepts sone soft of filter/query. May have a few overloads.
        /// Need to gain a better understanding of the underlying gRPC List/Scan APIs to design the most appropriate shape.
        /// May also have a `CountWorkflowsAsync(..)` equivalent if this makes sense from the query runtime perspective.
        /// </summary>        
        Task<IPaginatedReadOnlyCollectionPage<WorkflowConsecution>> ListWorkflowsAsync(NeedsDesign oneOrMoreArgs);

        /// <summary>
        /// Lists `WorkflowRun`s (not `WorkflowConsecution`s). Accepts sone soft of filter/query. May have a few overloads.
        /// Need to gain a better understanding of the underlying gRPC List/Scan APIs to design the most appropriate shape.
        /// May also have a `CountWorkflowRunsAsync(..)` equivalent if this makes sense from the query runtime perspective.
        /// </summary>        
        Task<IPaginatedReadOnlyCollectionPage<WorkflowConsecution>> ListWorkflowRunsAsync(NeedsDesign oneOrMoreArgs);

        // Research notes:
        // These are direct mappings of existing gRPC APIs for listing/scanning Workflow Runs:
        //Task<NeedsDesign> ListOpenWorkflowRunsAsync(NeedsDesign oneOrMoreArgs);
        //Task<NeedsDesign> ListClosedWorkflowRunsAsync(NeedsDesign oneOrMoreArgs);
        //Task<NeedsDesign> ListWorkflowRunsAsync(NeedsDesign oneOrMoreArgs);
        //Task<NeedsDesign> ListArchivedWorkflowRunsAsync(NeedsDesign oneOrMoreArgs);
        //Task<NeedsDesign> ScanWorkflowRunsAsync(NeedsDesign oneOrMoreArgs);  // Difference ListWorkflowsAsync vs. ScanWorkflowsAsync
        //Task<NeedsDesign> CountWorkflowRunsAsync(NeedsDesign oneOrMoreArgs);  // What exactly does the GRPC API count?
        #endregion -- Workflow listing APIs --


        #region GetWorkflowStub<TStub>(..)
        /// <summary>
        /// A stub returned by these methods can always be cast to 'IWorkflowConsecutionStub'.
        /// Initially, the stub is not bound to any workflow consecution.
        /// It will be bound when a stub's run-method API is called:
        ///  - If a workflow consecution with the specified workflow-id is Running AND binding to Running consecutions is permitted,
        ///    then the stub will be bound to that consecution.
        ///  - If a workflow consecution with the specified workflow-id is Not-Running AND binding to new consecutions is permitted,
        ///    then a new consecution will be attempted to start, and if it succeeds, the stub will be bound to that new consecution.
        /// See docs for 'WorkflowXxxStubAttribute' for more detials on binding.
        ///
        /// NOTE: 'TStub' must be an Interface and ANY interface can be specified.
        /// It is not required that the workflow implementation hosted in the worker implements this interface:
        ///  - The name of the workflow type is provided by this Workflow class instance.
        ///  - The names of the signals/queries will be deduced from the WorkflowXxxStub attributes.
        ///  - Parameters will be sent as specified.
        /// Mismatches will result in descriptive runtime errors (e.g. "no such signal handler").
        /// This makes it very easy to create interfaces for workflow implemented in any language, not necessarily a .NET language.
        /// Note that even for .NET-based workflows, the user may choose not to implement a client side iface by the workflow implementation.
        /// E.g., workflow handlers can take 'WorkflowContext' parameters, which are not part of the client-side iface.
        /// </summary>
        TStub CreateUnboundWorkflowStub<TStub>(string workflowTypeName,
                                               string workflowId,
                                               string taskQueue);

        TStub CreateUnboundWorkflowStub<TStub>(string workflowTypeName,
                                               string workflowId,
                                               string taskQueue,
                                               WorkflowConsecutionStubConfiguration stubConfig);

        TStub CreateUnboundWorkflowStub<TStub>(string workflowTypeName,
                                               string workflowId,
                                               IWorkflowExecutionConfiguration workflowConfig,
                                               WorkflowConsecutionStubConfiguration stubConfig,
                                               WorkflowConsecutionClientConfiguration clientConfig);
        #endregion GetWorkflowStub<TStub>(..)


        #region -- Server management APIs (not scoped to the bound namespace) --
        // These will not be part of V-Alpha.
        // Long-term they may be exposed either the underlyng GRPC cleint or here.
        // Expposing them here has some advantages:
        //  - All Temporal server access in in one place.
        //  - Flexibility to replace the common .NET gRPC client with Temporal Core to take advantage of the 
        //    shared functionality (metrics, reties, etc..)
        Task<NeedsDesign> GetClusterInfoAsync(NeedsDesign oneOrMoreArgs);
        Task<NeedsDesign> GetSystemInfoAsync(NeedsDesign oneOrMoreArgs);
        Task<NeedsDesign> GetSearchAttributesAsync(NeedsDesign oneOrMoreArgs);

        Task<NeedsDesign> RegisterNamespaceAsync(NeedsDesign oneOrMoreArgs);
        Task<NeedsDesign> ListNamespacesAsync(NeedsDesign oneOrMoreArgs);
        #endregion -- Server management APIs (not scoped to the bound namespace) --


        #region -- Server management APIs (scoped to the bound namespace) --
        // These will not be part of V-Alpha.
        // Same considerations apply as for the "server management APIs NOT scoped to the bound namespace" above.
        Task<NeedsDesign> DescribeTaskQueueAsync(NeedsDesign oneOrMoreArgs);
        #endregion -- Server management APIs (scoped to the bound namespace) --


        #region -- Namespace access APIs --
        // These will not be part of V-Alpha. Same considerations apply as for the "server management APIs" above.
        // Each Namespace access API has overloads that apply to the namespace "bound by the client" (see above)
        // and equivalent overloads that allow the namespace as a parameter.
        Task<NeedsDesign> DescribeNamespaceAsync(NeedsDesign oneOrMoreArgs);
        Task<NeedsDesign> DescribeNamespaceAsync(string @namespace, NeedsDesign oneOrMoreArgs);

        Task<NeedsDesign> UpdateNamespaceAsync(NeedsDesign oneOrMoreArgs);
        Task<NeedsDesign> UpdateNamespaceAsync(string @namespace, NeedsDesign oneOrMoreArgs);

        Task<NeedsDesign> DeprecateNamespaceAsync(NeedsDesign oneOrMoreArgs);  // Depricated in proto. Need it?
        Task<NeedsDesign> DeprecateNamespaceAsync(string @namespace, NeedsDesign oneOrMoreArgs);  // Depricated in proto. Need it?
        #endregion -- Namespace access APIs --
    }

    public class TemporalServiceClient : ITemporalServiceClient
    {
        private static TemporalServiceClientConfiguration CreateDefaultConfiguration() { return new TemporalServiceClientConfiguration(); }

        public static async Task<TemporalServiceClient> CreateNewAndInitializeConnectionAsync()
        {
            TemporalServiceClient client = new();
            await client.InitializeConnectionAsync();
            return client;
        }

        public static async Task<TemporalServiceClient> CreateNewAndInitializeConnectionAsync(TemporalServiceClientConfiguration config)
        {
            TemporalServiceClient client = new(config);
            await client.InitializeConnectionAsync();
            return client;
        }

        public TemporalServiceClient() : this(CreateDefaultConfiguration()) { }

        public TemporalServiceClient(TemporalServiceClientConfiguration config) { }

        public bool IsConectionInitialized { get; }

        /// <summary>
        /// Before the client can talk to the Temporal server, it must execute GetSystemInfo(..) to check server health
        /// and get server capabilities. This API will explicitly perform that. If this is not done explicitly, it will happen
        /// automatically before placing any other calls. <br />
        /// This will also validate connection to the namespace if it was set in the ctor.
        /// </summary>
        public Task InitializeConnectionAsync() { return null; }

        #region --- --- Interface implementation --- ---

        // ** All clarifications and comments are in the interface definition.
        // ** This section is just to make things compile for now.

        #region -- Namespace settings for the client --
        public string Namespace { get; }
        public Task<bool> TrySetNamespaceAsync(string @namespace) { return null; }
        public Task<bool> TrySetNamespaceAsync(string @namespace, CancellationToken cancelToken) { return null; }
        public Task<bool> IsNamespaceAccessibleAsync(string @namespace, CancellationToken cancelToken) { return null; }
        #endregion -- Namespace settings for the client --

        #region -- Workflow access and control APIs --

        #region StartNewWorkflowAsync(..)
        public Task<WorkflowConsecution> StartNewWorkflowAsync(string workflowTypeName,
                                                               string workflowId,
                                                               string taskQueue) { return null; }
        public Task<WorkflowConsecution> StartNewWorkflowAsync(string workflowTypeName,
                                                               string workflowId,
                                                               string taskQueue,
                                                               CancellationToken cancelToken) { return null; }
        public Task<WorkflowConsecution> StartNewWorkflowAsync(string workflowTypeName,
                                                               string workflowId,
                                                               string taskQueue,
                                                               IDataValue inputArgs) { return null; }
        public Task<WorkflowConsecution> StartNewWorkflowAsync(string workflowTypeName,
                                                               string workflowId,
                                                               string taskQueue,
                                                               IDataValue inputArgs,
                                                               CancellationToken cancelToken) { return null; }

        public Task<WorkflowConsecution> StartNewWorkflowAsync(string workflowTypeName,
                                                               string workflowId,
                                                               IWorkflowExecutionConfiguration workflowConfig,
                                                               IDataValue inputArgs,
                                                               WorkflowConsecutionClientConfiguration clientConfig,
                                                               CancellationToken cancelToken) { return null; }
        #endregion StartNewWorkflowAsync(..)

        #region StartNewWorkflowWithSignalAsync(..)
        public Task<WorkflowConsecution> StartNewWorkflowWithSignalAsync(string workflowTypeName,
                                                                         string workflowId,
                                                                         string taskQueue,
                                                                         string signalName,
                                                                         CancellationToken cancelToken) { return null; }

        public Task<WorkflowConsecution> StartNewWorkflowWithSignalAsync(string workflowTypeName,
                                                                         string workflowId,
                                                                         string taskQueue,
                                                                         string signalName,
                                                                         IDataValue signalArgs,
                                                                         CancellationToken cancelToken) { return null; }

        public Task<WorkflowConsecution> StartNewWorkflowWithSignalAsync(string workflowTypeName,
                                                                         string workflowId,
                                                                         string taskQueue,
                                                                         IDataValue wokflowArgs,
                                                                         string signalName,
                                                                         CancellationToken cancelToken) { return null; }

        public Task<WorkflowConsecution> StartNewWorkflowWithSignalAsync(string workflowTypeName,
                                                                         string workflowId,
                                                                         string taskQueue,
                                                                         IDataValue wokflowArgs,
                                                                         string signalName,
                                                                         IDataValue signalArgs,
                                                                         CancellationToken cancelToken) { return null; }

        public Task<WorkflowConsecution> StartNewWorkflowWithSignalAsync(string workflowTypeName,
                                                                         string workflowId,
                                                                         IWorkflowExecutionConfiguration workflowConfig,
                                                                         IDataValue wokflowArgs,
                                                                         string signalName,
                                                                         IDataValue signalArgs,
                                                                         WorkflowConsecutionClientConfiguration clientConfig,
                                                                         CancellationToken cancelToken) { return null; }
        #endregion StartNewWorkflowWithSignalAsync(..)

        #region GetOrStartWorkflowAsync(..)
        public Task<WorkflowConsecution> GetOrStartWorkflowAsync(string workflowTypeName,
                                                                 string workflowId,
                                                                 string taskQueue) { return null; }

        public Task<WorkflowConsecution> GetOrStartWorkflowAsync(string workflowTypeName,
                                                                 string workflowId,
                                                                 string taskQueue,
                                                                 CancellationToken cancelToken) { return null; }

        public Task<WorkflowConsecution> GetOrStartWorkflowAsync(string workflowTypeName,
                                                                 string workflowId,
                                                                 string taskQueue,
                                                                 IDataValue inputArgs,
                                                                 CancellationToken cancelToken) { return null; }

        public Task<WorkflowConsecution> GetOrStartWorkflowAsync(string workflowTypeName,
                                                                 string workflowId,
                                                                 IWorkflowExecutionConfiguration workflowConfig,
                                                                 IDataValue inputArgs,
                                                                 WorkflowConsecutionClientConfiguration clientConfig,
                                                                 CancellationToken cancelToken) { return null; }
        #endregion GetOrStartWorkflowAsync(..)

        #region GetWorkflowAsync(..)
        public Task<WorkflowConsecution> GetWorkflowAsync(string workflowId) { return null; }

        public Task<WorkflowConsecution> GetWorkflowAsync(string workflowId,
                                                          CancellationToken cancelToken) { return null; }

        public Task<WorkflowConsecution> GetWorkflowAsync(string workflowId,
                                                          string workflowConsecutionId,                                                                  
                                                          CancellationToken cancelToken) { return null; }

        public Task<WorkflowConsecution> GetWorkflowAsync(string workflowTypeName,
                                                          string workflowId,
                                                          string workflowConsecutionId,
                                                          WorkflowConsecutionClientConfiguration clientConfig,
                                                          CancellationToken cancelToken) { return null; }

        public Task<TryGetResult<WorkflowConsecution>> TryGetWorkflowAsync(string workflowId) { return null; }

        public Task<TryGetResult<WorkflowConsecution>> TryGetWorkflowAsync(string workflowTypeName,
                                                                           string workflowId,
                                                                           string workflowConsecutionId,
                                                                           WorkflowConsecutionClientConfiguration clientConfig,
                                                                           CancellationToken cancelToken) { return null; }
        #endregion GetWorkflowAsync(..)

        #region GetWorkflowRunAsync(..)
        public Task<WorkflowRun> GetWorkflowRunAsync(string workflowRunId,
                                                     CancellationToken cancelToken) { return null; }

        public Task<WorkflowRun> GetWorkflowRunAsync(string workflowId,
                                                     string workflowRunId,
                                                     CancellationToken cancelToken) { return null; }

        public Task<WorkflowRun> GetWorkflowRunAsync(string workflowId,
                                                     string workflowRunId,
                                                     WorkflowConsecutionClientConfiguration clientConfig,
                                                     CancellationToken cancelToken) { return null; }
        #endregion GetWorkflowRunAsync(..)
        #endregion -- Workflow access and control APIs --

        #region -- Workflow listing APIs --
        public Task<IPaginatedReadOnlyCollectionPage<WorkflowConsecution>> ListWorkflowsAsync(NeedsDesign oneOrMoreArgs) { return null; }
        public Task<IPaginatedReadOnlyCollectionPage<WorkflowConsecution>> ListWorkflowRunsAsync(NeedsDesign oneOrMoreArgs) { return null; }
        #endregion -- Workflow listing APIs --


        #region GetWorkflowStub<TStub>(..)
        public TStub CreateUnboundWorkflowStub<TStub>(string workflowTypeName,
                                                      string workflowId,
                                                      string taskQueue) { return default(TStub); }

        public TStub CreateUnboundWorkflowStub<TStub>(string workflowTypeName,
                                                      string workflowId,
                                                      string taskQueue,
                                                      WorkflowConsecutionStubConfiguration stubConfig) { return default(TStub); }

        public TStub CreateUnboundWorkflowStub<TStub>(string workflowTypeName,
                                                      string workflowId,
                                                      IWorkflowExecutionConfiguration workflowConfig,
                                                      WorkflowConsecutionStubConfiguration stubConfig,
                                                      WorkflowConsecutionClientConfiguration clientConfig) { return default(TStub); }
        #endregion GetWorkflowStub<TStub>(..)


        #region -- Server management APIs (not scoped to the bound namespace) --
        public Task<NeedsDesign> GetClusterInfoAsync(NeedsDesign oneOrMoreArgs) { return null; }
        public Task<NeedsDesign> GetSystemInfoAsync(NeedsDesign oneOrMoreArgs) { return null; }
        public Task<NeedsDesign> GetSearchAttributesAsync(NeedsDesign oneOrMoreArgs) { return null; }

        public Task<NeedsDesign> RegisterNamespaceAsync(NeedsDesign oneOrMoreArgs) { return null; }
        public Task<NeedsDesign> ListNamespacesAsync(NeedsDesign oneOrMoreArgs) { return null; }
        #endregion -- Server management APIs (not scoped to the bound namespace) --


        #region -- Server management APIs (scoped to the bound namespace) --
        public Task<NeedsDesign> DescribeTaskQueueAsync(NeedsDesign oneOrMoreArgs) { return null; }
        #endregion -- Server management APIs (scoped to the bound namespace) --


        #region -- Namespace access APIs --
        public Task<NeedsDesign> DescribeNamespaceAsync(NeedsDesign oneOrMoreArgs) { return null; }
        public Task<NeedsDesign> DescribeNamespaceAsync(string @namespace, NeedsDesign oneOrMoreArgs) { return null; }

        public Task<NeedsDesign> UpdateNamespaceAsync(NeedsDesign oneOrMoreArgs) { return null; }
        public Task<NeedsDesign> UpdateNamespaceAsync(string @namespace, NeedsDesign oneOrMoreArgs) { return null; }

        public Task<NeedsDesign> DeprecateNamespaceAsync(NeedsDesign oneOrMoreArgs) { return null; }
        public Task<NeedsDesign> DeprecateNamespaceAsync(string @namespace, NeedsDesign oneOrMoreArgs) { return null; }
        #endregion -- Namespace access APIs --

        #endregion --- --- Interface implementation --- ---
    }

    #endregion TemporalServiceClient


    #region class WorkflowConsecution

    /// <summary>
    /// Final type name is pending the terminology discussion.
    /// </summary>
    public class WorkflowConsecution
    {
        public string Namespace { get; }
        public string WorkflowTypeName { get; }
        public string WorkflowId { get; }

        /// <summary>Id of the first run in the concecution.</summary>
        public string WorkflowConsecutionId { get; }

        /// <summary>Gets the `TemporalServiceClient` that created this `WorkflowConsecution`.</summary>
        /// <remarks>Do we need this? What is the scenario? Can it break encapsulation?</remarks>
        public TemporalServiceClient ServiceClient { get; }

        public Task<bool> IsRunningAsync() { return null; }

        /// <summary>The returned stub is bound to this workflow consecution.</summary>
        /// <seealso cref="TemporalServiceClient.CreateWorkflowStub{TStub}(String, String, String, CancellationToken)" />
        /// <remarks>See docs for `WorkflowXxxStubAttribute` for more detials on binding.</remarks>
        public TStub GetStub<TStub>() { return default(TStub); }

        #region --- GetXxxRunAsync(..) APIs to access a specific run ---

        /// <summary>Get the run with the specified run-id. Throw if not found.</summary>
        public Task<WorkflowRun> GetRunAsync(string workflowRunId, CancellationToken cancelToken) { return null; }

        /// <summary>Get the run with the specified run-id. Return false if not found.</summary>
        public Task<TryGetResult<WorkflowRun>> TryGetRunAsync(string workflowRunId, CancellationToken cancelToken) { return null; }

        /// <summary>Get the first / initial run in this consecution.</summary>
        public Task<WorkflowRun> GetFirstRunAsync(CancellationToken cancelToken) { return null; }

        /// <summary>Get the most recent run in this consecution.</summary>
        public Task<WorkflowRun> GetLatestRunAsync(CancellationToken cancelToken) { return null; }

        /// <summary>
        /// Get the very last run IF it is already known to be final (no further runs can/will follow).<br />
        /// If it is not yet known whether the latest run is final, this API will not fail, but it will return False.
        /// </summary>        
        public Task<TryGetResult<WorkflowRun>> TryGetFinalRunAsync(CancellationToken cancelToken) { return null; }

        #endregion --- GetXxxRunAsync(..) APIs to access a specific run ---

        /// <summary>Lists runs of this consicution only. Needs overloads with filters? Not in V-Alpha.</summary>
        public Task<IPaginatedReadOnlyCollectionPage<WorkflowRun>> ListRunsAsync(NeedsDesign oneOrMoreArgs) { return null; }

        #region --- APIs to interact with the consecution ---

        // Invoking these APIs will interact with the currently active (aka latest, aka running) Run in this consecution.
        // In all common scenarios this is what you want.
        // In some rare scenarios when you need to interact with a specific Run, obrain the corresponding WorkflowRun instance
        // and invoke the corresponding API on that instance.

        /// <summary>The returned task completes when this consecution finishes (incl. any runs not yet started). Performs long poll.</summary>
        public Task<IWorkflowConsecutionResult> GetResultAsync() { return null; }
        public Task<IWorkflowConsecutionResult> GetResultAsync(CancellationToken cancelToken) { return null; }
        public Task<IWorkflowConsecutionResult<TResult>> GetResultAsync<TResult>() where TResult : IDataValue { return null; }
        public Task<IWorkflowConsecutionResult<TResult>> GetResultAsync<TResult>(CancellationToken cancelToken) where TResult : IDataValue { return null; }

        /// <summary>Get result if consecution has finished. Otherwise return False. No long poll.</summary>
        public Task<TryGetResult<IWorkflowConsecutionResult>> TryGetResultIfAvailableAync() { return null; }
        public Task<TryGetResult<IWorkflowConsecutionResult>> TryGetResultIfAvailableAync(CancellationToken cancelToken) { return null; }
        public Task<TryGetResult<IWorkflowConsecutionResult<TResult>>> TryGetResultIfAvailableAync<TResult>(CancellationToken cancelToken) where TResult : IDataValue { return null; }

        public Task SignalAsync(string signalName, CancellationToken cancelToken) { return null; }
        public Task SignalAsync(string signalName, IDataValue arg) { return null; }
        public Task SignalAsync(string signalName, IDataValue arg, CancellationToken cancelToken) { return null; }

        public Task<TResult> QueryAsync<TResult>(string queryName) where TResult : IDataValue { return null; }
        public Task<TResult> QueryAsync<TResult>(string queryName, CancellationToken cancelToken) where TResult : IDataValue { return null; }
        public Task<TResult> QueryAsync<TResult>(string queryName, IDataValue arg, CancellationToken cancelToken) where TResult : IDataValue { return null; }

        public Task RequestCancellationAsync() { return null; }
        public Task RequestCancellationAsync(CancellationToken cancelToken) { return null; }

        public Task TerminateAsync(string reason) { return null; }
        public Task TerminateAsync(string reason, IDataValue details, CancellationToken cancelToken) { return null; }

        #endregion --- APIs to interact with the consecution ---
    }
    #endregion class WorkflowConsecution


    #region class WorkflowRun
    public class WorkflowRun
    {
        public string Namespace { get; }
        public string WorkflowTypeName { get; }
        public string WorkflowId { get; }
        public string WorkflowRunId { get; }

        public Task<WorkflowConsecution> GetOwnerWorkflowAsync(CancellationToken cancelToken) { return null; }

        public Task<bool> IsRunningAsync() { return null; }

        public Task<WorkflowRunInfo> GetInfoAsync() { return null; }

        #region --- APIs to interact with the run ---

        // Invoking these APIs will interact with the this Workflow Run in this consecution.
        // In all common scenarios this actually NOT what you want.
        // Instead, you want the corresponding API on WorkflowConsecution, which will automatically select the
        // latest run withn the consecution and interact with that.
        // In some rare scenarios when you need to interact with a specific Run, use the APIs below.

        /// <summary>The returned task completes when this consecution finishes (incl. any runs not yet started). Performs long poll.</summary>        
        public Task<IWorkflowRunResult> GetResultAsync(CancellationToken cancelToken) { return null; }
        public Task<IWorkflowRunResult<TResult>> GetResultAsync<TResult>(CancellationToken cancelToken) where TResult : IDataValue { return null; }

        /// <summary>Get result if consecution has finished. Otherwise return False. No long poll.</summary>        
        public Task<TryGetResult<IWorkflowRunResult>> TryGetResultIfAvailableAync(CancellationToken cancelToken) { return null; }
        public Task<TryGetResult<IWorkflowRunResult<TResult>>> TryGetResultIfAvailableAync<TResult>(CancellationToken cancelToken) where TResult : IDataValue { return null; }

        public Task SignalAsync(string signalName, CancellationToken cancelToken) { return null; }
        public Task SignalAsync(string signalName, IDataValue arg, CancellationToken cancelToken) { return null; }

        public Task<TResult> QueryAsync<TResult>(string queryName, CancellationToken cancelToken) where TResult : IDataValue { return null; }
        public Task<TResult> QueryAsync<TResult>(string queryName, IDataValue arg, CancellationToken cancelToken) where TResult : IDataValue { return null; }

        public Task RequestCancellationAsync(CancellationToken cancelToken) { return null; }

        public Task TerminateAsync(string reason) { return null; }
        public Task TerminateAsync(string reason, IDataValue details, CancellationToken cancelToken) { return null; }

        #endregion --- APIs to interact with the run ---
    }
    #endregion class WorkflowRun

    public sealed class WorkflowRunInfo
    {
        // @ToDo. Roughly corresponds to DescribeWorkflowExecutionResponse.
    }

    public interface IWorkflowRunResultBase
    {
        PayloadsCollection ResultPayload { get; }
        bool IsCompletedNormally { get; }
        WorkflowExecutionStatus Status { get; }
        Exception Failure { get; }
        IDataValue GetValue(); // Wraps if result was not IDataValue. Returns IDataValue.Void.Instance if there is no result value.
    }

    public interface IWorkflowRunResultBase<out TResult> where TResult : IDataValue
    {
        TResult Value { get; }
    }

    public interface IWorkflowConsecutionResult : IWorkflowRunResultBase
    {        
    }

    public interface IWorkflowConsecutionResult<out TResult> : IWorkflowRunResultBase<TResult> where TResult : IDataValue
    {
    }

    public interface IWorkflowRunResult : IWorkflowRunResultBase
    {
        RetryState RetryState { get; }
        bool IsContinuedAsNew { get; }
    }

    public interface IWorkflowRunResult<out TResult> : IWorkflowRunResultBase<TResult>, IWorkflowRunResult where TResult : IDataValue
    {
        Task<TryGetResult<WorkflowRun>> TryGetContinuationRunAsync();
    }

    public class TemporalServiceClientConfiguration
    {
        public String ServiceUrl { get; init; }
        public bool IsHttpsEnabled { get; init; }
        public string Namespace { get; init; }

        /// <summary>
        /// Factory gets namespace, workflow type name, and workflow id; returns a new non-null data converter to be
        /// applied to ALL calls made by the client constructued with this config.
        /// </summary>
        public Func<string, string, string, IDataConverter> DataConverterFactory { get; init; }

        /// <summary>
        /// Factory gets namespace, workflow type name, workflow id, and a list of all already existing interceptors,
        /// i.e. the interceptors generated by the system. That interceptor list is never null, but may be empty.
        /// The interceptor list does NOT include the final "sink" interceptor - it will always remain LAST and the factory 
        /// shall not be able to affect that. However, the list DOES include all other system interceptors (e.g. the ones
        /// that implement distributed tracing. The factory may modify that list any way it wants, including removing
        /// system interceptors (expect the aforementioned sink) or adding new interceptors before or after.
        /// Nulls must not be added to the list.
        /// </summary>
        public Action<string, string, string, IList<ITemporalServiceClientInterceptor>> TemporalServiceClientInterceptorFactory { get; init; }

        // . . .
    }

    
    public class WorkflowConsecutionClientConfiguration
    {
        /// <summary>
        /// Data converter for this particular workflow client (repalces a more global data converter).
        /// </summary>
        public IDataConverter DataConverter { get; set; }

        /// <summary>
        /// Factory gets a list of all already existing interceptors, i.e. the initial list generated by the system and later
        /// processed byt the TemporalServiceClient's TemporalServiceClientInterceptorFactory (if it was set).
        /// The list is a shallow copy of the TemporalServiceClient's list. The factory shall modify it and the result will apply
        /// to the respective `Workflow` instance only and all runs that belong to it.        
        /// See <see cref="TemporalServiceClientConfiguration.TemporalServiceClientFactory" /> for details about handling and
        /// modifying the interceptor list.
        /// </summary>
        public Action<IList<ITemporalServiceClientInterceptor>> TemporalServiceClientInterceptorFactory { get; set; }

        // . . .
    }

    // ----------- -----------

    public interface IWorkflowConsecutionStub
    {
        WorkflowConsecutionStubConfiguration Config { get; }
        bool IsBound { get; }        
        bool TryGetWorkflow(out WorkflowConsecution workflow);

        /// <summary>Bind now or return readily bound consecution.</summary>
        Task<WorkflowConsecution> EnsureIsBoundAsync();
        Task<WorkflowConsecution> EnsureIsBoundAsync(WorkflowConsecutionStubConfiguration stubConfig);
        Task<WorkflowConsecution> EnsureIsBoundAsync(IDataValue inputArgs,
                                                     WorkflowConsecutionStubConfiguration stubConfig,
                                                     CancellationToken cancelToken);
    }

    // ----------- -----------
    
    public sealed class WorkflowConsecutionStubConfiguration
    {
        public bool CanBindToNewConsecution { get; init; }
        public bool CanBindToExistingRunningConsecution { get; init; }
        public bool CanBindToExistingFinishedConsecution { get; init; }        

        public WorkflowConsecutionStubConfiguration()
            : this(canBindToNewConsecution: true, canBindToExistingRunningConsecution: true, canBindToExistingFinishedConsecution: true) { }

        public WorkflowConsecutionStubConfiguration(bool canBindToNewConsecution, bool canBindToExistingRunningConsecution, bool canBindToExistingFinishedConsecution)
        {
            CanBindToNewConsecution = canBindToNewConsecution;
            CanBindToExistingRunningConsecution = canBindToExistingRunningConsecution;
            CanBindToExistingFinishedConsecution = canBindToExistingFinishedConsecution;         
        }
    }

    // ----------- -----------

    /// <summary>
    /// One instance if this interface will be creeated PER WorkflowConsecution instance.
    /// Implementations need to design/use local state accordingly.
    /// 
    /// If we rename types `WorkflowConsecution` or `WorkflowRun`, rename this type and its APIs accordingly.
    /// This iface has one OnXxx API for each "primary" method on `WorkflowConsecution` and `WorkflowRun`.
    /// If a method has several overloads that delegate to one "most complete" overload, there is
    /// only a handler for that most complete overload.
    /// 
    /// The interceptor must have handlers for all relevant APIs where we support interception.
    /// For now we define only a subset of them, so that we have less work when the APIs of
    /// TemporalServiceClient, WorkflowConsecution and WorkflowRun change. We'll add the rest as soon as those APIs consolidate.
    /// </summary>
    public interface ITemporalServiceClientInterceptor
    {
        #region ---  Interceptor controll APIs ---
        void Init(string @namespace, string workflowTypeName, string workflowId, ITemporalServiceClientInterceptor nextInterceptor);

        ITemporalServiceClientInterceptor.IWorkflowConsecutionInterceptor ConsecutionCallsInterceptor { get; }
        ITemporalServiceClientInterceptor.IWorkflowRunInterceptor RunCallsInterceptor { get; }

        #endregion ---  Interceptor controll APIs ---

        #region ---  TemporalServiceClient API interceptors ---

        Task<WorkflowConsecution> OnStartNewWorkflowWithSignalAsync(string workflowTypeName,
                                                                    string workflowId,
                                                                    IWorkflowExecutionConfiguration workflowConfig,
                                                                    IDataValue wokflowArgs,
                                                                    string signalName,
                                                                    IDataValue signalArgs,
                                                                    WorkflowConsecutionClientConfiguration clientConfig,
                                                                    CancellationToken cancelToken);
        Task<WorkflowConsecution> OnGetOrStartWorkflowAsync(string workflowTypeName,
                                                            string workflowId,
                                                            IWorkflowExecutionConfiguration workflowConfig,
                                                            IDataValue inputArgs,
                                                            WorkflowConsecutionClientConfiguration clientConfig,
                                                            CancellationToken cancelToken);

        Task<WorkflowConsecution> OnGetWorkflowAsync(string workflowTypeName,
                                                    string workflowId,
                                                    string workflowConsecutionId,
                                                    WorkflowConsecutionClientConfiguration clientConfig,
                                                    CancellationToken cancelToken);
        Task<WorkflowRun> OnGetWorkflowRunAsync(string workflowId,
                                                string workflowRunId,
                                                WorkflowConsecutionClientConfiguration clientConfig,
                                                CancellationToken cancelToken);

        // ...
        #endregion ---  TemporalServiceClient API interceptors ---

        /// <summary>Just for semantic grouping.
        /// There will be exact one instance of this per `ITemporalServiceClientInterceptor` and there will be one instance of 
        /// `ITemporalServiceClientInterceptor` per instance of `WorkflowConsecution`.</summary>
        public interface IWorkflowConsecutionInterceptor
        {
            void Init(ITemporalServiceClientInterceptor owner);
            Task<WorkflowRun> OnGetRunAsync(string workflowRunId, CancellationToken cancelToken);
            Task<WorkflowRun> OnGetLatestRunAsync(CancellationToken cancelToken);
            Task<IWorkflowConsecutionResult<TResult>> OnGetResultAsync<TResult>(CancellationToken cancelToken) where TResult : IDataValue;
            Task OnSignalAsync(string signalName, IDataValue arg, CancellationToken cancelToken);
            Task OnTerminateAsync(string reason, IDataValue details, CancellationToken cancelToken);

            // ...
        }

        /// <summary>Just for semantic grouping.
        /// There will be exact one instance of this per `ITemporalServiceClientInterceptor` and there will be one instance of 
        /// `ITemporalServiceClientInterceptor` per instance of `WorkflowConsecution`.</summary>
        public interface IWorkflowRunInterceptor
        {
            void Init(ITemporalServiceClientInterceptor owner);
            Task<WorkflowRunInfo> OnGetInfoAsync();
            Task<IWorkflowRunResult<TResult>> OnGetResultAsync<TResult>(CancellationToken cancelToken) where TResult : IDataValue;
            Task<TResult> OnQueryAsync<TResult>(string queryName, IDataValue arg, CancellationToken cancelToken) where TResult : IDataValue;
            Task OnRequestCancellationAsync(CancellationToken cancelToken);
            // ...
        }
    }

    public abstract class TemporalServiceClientInterceptorBase : ITemporalServiceClientInterceptor
    {
        private ITemporalServiceClientInterceptor _nextInterceptor = null;

        protected string Namespace { get; private set; }
        protected string WorkflowTypeName { get; private set; }
        protected string WorkflowId { get; private set; }
        protected WorkflowConsecution Workflow { get; private set; }

        protected ITemporalServiceClientInterceptor NextInterceptor
        {
            get
            {
                if (_nextInterceptor == null)
                {
                    throw new InvalidOperationException($"The {nameof(NextInterceptor)} of this {this.GetType().Name} instance is null."
                                                       + " Was this instance initialized?");
                }

                return _nextInterceptor;
            }
        }

        #region ---  Interceptor controll APIs ---
        public ITemporalServiceClientInterceptor.IWorkflowConsecutionInterceptor ConsecutionCallsInterceptor { get; private set; }
        public ITemporalServiceClientInterceptor.IWorkflowRunInterceptor RunCallsInterceptor { get; private set; }

        public virtual void Init(string @namespace, string workflowTypeName, string workflowId, ITemporalServiceClientInterceptor nextInterceptor)
        {
            Namespace = @namespace;
            WorkflowTypeName = workflowTypeName;
            WorkflowId = workflowId;
            _nextInterceptor = nextInterceptor;

            ConsecutionCallsInterceptor = CreateConsecutionCallsInterceptor();
            if (ConsecutionCallsInterceptor != null)
            {
                ConsecutionCallsInterceptor.Init(this);
            }

            RunCallsInterceptor = CreatRunCallsInterceptor();
            if (RunCallsInterceptor != null)
            {
                RunCallsInterceptor.Init(this);
            }
        }

        protected virtual ITemporalServiceClientInterceptor.IWorkflowConsecutionInterceptor CreateConsecutionCallsInterceptor()
        {
            return new TemporalServiceClientInterceptorBase.WorkflowConsecutionInterceptorBaseImpl();
        }

        protected virtual ITemporalServiceClientInterceptor.IWorkflowRunInterceptor CreatRunCallsInterceptor()
        {
            return new TemporalServiceClientInterceptorBase.WorkflowRunInterceptorBaseImpl();
        }
        #endregion ---  Interceptor controll APIs ---

        // The TemporalServiceClient APIs interceptors that result in creating a new `WorkflowConsecution` instance must
        // initialize the `Workflow` field of this instance.
        public virtual async Task<WorkflowConsecution> OnStartNewWorkflowWithSignalAsync(string workflowTypeName,
                                                                                         string workflowId,
                                                                                         IWorkflowExecutionConfiguration workflowConfig,
                                                                                         IDataValue wokflowArgs,
                                                                                         string signalName,
                                                                                         IDataValue signalArgs,
                                                                                         WorkflowConsecutionClientConfiguration clientConfig,
                                                                                         CancellationToken cancelToken)
        {
            Workflow = await NextInterceptor.OnStartNewWorkflowWithSignalAsync(workflowTypeName,
                                                                               workflowId,
                                                                               workflowConfig,
                                                                               wokflowArgs,
                                                                               signalName,
                                                                               signalArgs,
                                                                               clientConfig,
                                                                               cancelToken);
            return Workflow;
        }

        public virtual async Task<WorkflowConsecution> OnGetOrStartWorkflowAsync(string workflowTypeName,
                                                                                 string workflowId,
                                                                                 IWorkflowExecutionConfiguration workflowConfig,
                                                                                 IDataValue inputArgs,
                                                                                 WorkflowConsecutionClientConfiguration clientConfig,
                                                                                 CancellationToken cancelToken)
        {
            Workflow = await NextInterceptor.OnGetOrStartWorkflowAsync(workflowTypeName,
                                                                       workflowId,
                                                                       workflowConfig,
                                                                       inputArgs,
                                                                       clientConfig,
                                                                       cancelToken);
            return Workflow;
        }

        public virtual async Task<WorkflowConsecution> OnGetWorkflowAsync(string workflowTypeName,
                                                                          string workflowId,
                                                                          string workflowConsecutionId,
                                                                          WorkflowConsecutionClientConfiguration clientConfig,
                                                                          CancellationToken cancelToken)
        {
            Workflow = await NextInterceptor.OnGetWorkflowAsync(workflowTypeName,
                                                                workflowId,
                                                                workflowConsecutionId,
                                                                clientConfig,
                                                                cancelToken);
            return Workflow;
        }
        public virtual Task<WorkflowRun> OnGetWorkflowRunAsync(string workflowId,
                                                               string workflowRunId,
                                                               WorkflowConsecutionClientConfiguration clientConfig,
                                                               CancellationToken cancelToken)
        {
            return NextInterceptor.OnGetWorkflowRunAsync(workflowId,
                                                         workflowRunId,
                                                         clientConfig,
                                                         cancelToken);
        }

        // ...

        private class WorkflowConsecutionInterceptorBaseImpl : WorkflowConsecutionInterceptorBase
        {
        }

        public abstract class WorkflowConsecutionInterceptorBase : ITemporalServiceClientInterceptor.IWorkflowConsecutionInterceptor
        {
            private TemporalServiceClientInterceptorBase _owner = null;

            protected TemporalServiceClientInterceptorBase Owner
            {
                get
                {
                    if (_owner == null)
                    {
                        throw new InvalidOperationException($"The {nameof(Owner)} of this {this.GetType().Name} instance is null."
                                                           + " Was this instance initialized?");
                    }

                    return _owner;
                }
            }

            public virtual void Init(TemporalServiceClientInterceptorBase owner)
            {
                _owner = owner;
            }

            public virtual void Init(ITemporalServiceClientInterceptor owner)
            {
                // check for null.
                if (owner is TemporalServiceClientInterceptorBase baseOwner)
                {
                    this.Init(baseOwner);
                    return;
                }

                throw new ArgumentException($"This instance of type \"{this.GetType().FullName}\""
                                          + $" (which is derived from \"{typeof(WorkflowConsecutionInterceptorBase).FullName}\")"
                                          + $" must be owned by an instance of a type that is derived from"
                                          + $" \"{typeof(TemporalServiceClientInterceptorBase).FullName}\". However, the specified"
                                          + $" {nameof(owner)} is an instance of type \"{owner.GetType().FullName}\", which is not"
                                          + $" derived in that way.");
            }

            public virtual Task<WorkflowRun> OnGetLatestRunAsync(CancellationToken cancelToken)
            {
                return Owner.NextInterceptor.ConsecutionCallsInterceptor.OnGetLatestRunAsync(cancelToken);
            }

            public virtual Task<IWorkflowConsecutionResult<TResult>> OnGetResultAsync<TResult>(CancellationToken cancelToken) where TResult : IDataValue
            {
                return Owner.NextInterceptor.ConsecutionCallsInterceptor.OnGetResultAsync<TResult>(cancelToken);
            }

            public virtual Task<WorkflowRun> OnGetRunAsync(string workflowRunId, CancellationToken cancelToken)
            {
                return Owner.NextInterceptor.ConsecutionCallsInterceptor.OnGetRunAsync(workflowRunId, cancelToken);
            }

            public virtual Task OnSignalAsync(string signalName, IDataValue arg, CancellationToken cancelToken)
            {
                return Owner.NextInterceptor.ConsecutionCallsInterceptor.OnSignalAsync(signalName, arg, cancelToken);
            }

            public virtual Task OnTerminateAsync(string reason, IDataValue details, CancellationToken cancelToken)
            {
                return Owner.NextInterceptor.ConsecutionCallsInterceptor.OnTerminateAsync(reason, details, cancelToken);
            }

            // ...
        }

        private class WorkflowRunInterceptorBaseImpl : WorkflowRunInterceptorBase
        {
        }

        public abstract class WorkflowRunInterceptorBase : ITemporalServiceClientInterceptor.IWorkflowRunInterceptor
        {
            private TemporalServiceClientInterceptorBase _owner = null;

            protected TemporalServiceClientInterceptorBase Owner
            {
                get
                {
                    if (_owner == null)
                    {
                        throw new InvalidOperationException($"The {nameof(Owner)} of this {this.GetType().Name} instance is null."
                                                           + " Was this instance initialized?");
                    }

                    return _owner;
                }
            }

            public virtual void Init(TemporalServiceClientInterceptorBase owner)
            {
                _owner = owner;
            }

            public virtual void Init(ITemporalServiceClientInterceptor owner)
            {
                // check for null.
                if (owner is TemporalServiceClientInterceptorBase baseOwner)
                {
                    this.Init(baseOwner);
                    return;
                }

                throw new ArgumentException($"This instance of type \"{this.GetType().FullName}\""
                                          + $" (which is derived from \"{typeof(WorkflowRunInterceptorBase).FullName}\")"
                                          + $" must be owned by an instance of a type that is derived from"
                                          + $" \"{typeof(TemporalServiceClientInterceptorBase).FullName}\". However, the specified"
                                          + $" {nameof(owner)} is an instance of type \"{owner.GetType().FullName}\", which is not"
                                          + $" derived in that way.");
            }

            public virtual Task<WorkflowRunInfo> OnGetInfoAsync()
            {
                return Owner.NextInterceptor.RunCallsInterceptor.OnGetInfoAsync();
            }

            public virtual Task<IWorkflowRunResult<TResult>> OnGetResultAsync<TResult>(CancellationToken cancelToken) where TResult : IDataValue
            {
                return Owner.NextInterceptor.RunCallsInterceptor.OnGetResultAsync<TResult>(cancelToken);
            }

            public virtual Task<TResult> OnQueryAsync<TResult>(string queryName, IDataValue arg, CancellationToken cancelToken) where TResult : IDataValue
            {
                return Owner.NextInterceptor.RunCallsInterceptor.OnQueryAsync<TResult>(queryName, arg, cancelToken);
            }

            public virtual Task OnRequestCancellationAsync(CancellationToken cancelToken)
            {
                return Owner.NextInterceptor.RunCallsInterceptor.OnRequestCancellationAsync(cancelToken);
            }

            // ...
        }
    }

    // ----------- -----------

    public class NeedsDesign
    {
        // Placeholder. @ToDo.
    }

    public class NeedsDesignException : Exception
    {
        // Placeholder. @ToDo.
    }
}
