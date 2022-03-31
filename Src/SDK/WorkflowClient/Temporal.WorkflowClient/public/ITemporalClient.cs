using System;
using System.Threading;
using System.Threading.Tasks;
using Temporal.Common;
using Temporal.WorkflowClient.Interceptors;

namespace Temporal.WorkflowClient
{
    public interface ITemporalClient
    {
        #region -- Common properties --

        string Namespace { get; }

        #endregion -- Common properties --


        #region -- Workflow access and control APIs --

        #region StartWorkflowAsync(..)

        Task<IWorkflowChain> StartWorkflowAsync(string workflowId,
                                                string workflowTypeName,
                                                string taskQueue,
                                                StartWorkflowChainConfiguration workflowConfig = null,
                                                CancellationToken cancelToken = default);

        Task<IWorkflowChain> StartWorkflowAsync<TWfArg>(string workflowId,
                                                        string workflowTypeName,
                                                        string taskQueue,
                                                        TWfArg workflowArg,
                                                        StartWorkflowChainConfiguration workflowConfig = null,
                                                        CancellationToken cancelToken = default);
        #endregion StartWorkflowAsync(..)


        #region StartWorkflowWithSignalAsync(..)

        Task<IWorkflowChain> StartWorkflowWithSignalAsync(string workflowId,
                                                          string workflowTypeName,
                                                          string taskQueue,
                                                          string signalName,
                                                          StartWorkflowChainConfiguration workflowConfig = null,
                                                          CancellationToken cancelToken = default);

        Task<IWorkflowChain> StartWorkflowWithSignalAsync<TSigArg>(string workflowId,
                                                                   string workflowTypeName,
                                                                   string taskQueue,
                                                                   string signalName,
                                                                   TSigArg signalArg,
                                                                   StartWorkflowChainConfiguration workflowConfig = null,
                                                                   CancellationToken cancelToken = default);

        Task<IWorkflowChain> StartWorkflowWithSignalAsync<TWfArg>(string workflowId,
                                                                  string workflowTypeName,
                                                                  string taskQueue,
                                                                  TWfArg workflowArg,
                                                                  string signalName,
                                                                  StartWorkflowChainConfiguration workflowConfig = null,
                                                                  CancellationToken cancelToken = default);

        Task<IWorkflowChain> StartWorkflowWithSignalAsync<TWfArg, TSigArg>(string workflowId,
                                                                           string workflowTypeName,
                                                                           string taskQueue,
                                                                           TWfArg workflowArg,
                                                                           string signalName,
                                                                           TSigArg signalArg,
                                                                           StartWorkflowChainConfiguration workflowConfig = null,
                                                                           CancellationToken cancelToken = default);

        #endregion StartWorkflowWithSignalAsync(..)


        #region CreateWorkflowHandle(..)        

        /// <summary>
        /// Create an unbound workflow chain handle.
        /// The handle will be bound to the most recent chain with the specified <c>workflowId</c> once the user invokes an API
        /// the end up interacting with some workflow run on the server.
        /// </summary>
        IWorkflowChain CreateWorkflowHandle(string workflowId);

        /// <summary>
        /// Create an workflow chain handle that represents a workflow chain with the specified <c>workflowId</c> and <c>workflowChainId</c>.
        /// </summary>
        /// <param name="workflowId">The workflow-id of the workflow chain that will be represented by the newly created handle.</param>
        /// <param name="workflowChainId">The workflow-run-id of the <em>first workflow run</em> of the <em>workflow chain</em> represented
        /// by the newly created handle.</param>
        IWorkflowChain CreateWorkflowHandle(string workflowId,
                                            string workflowChainId);
        #endregion CreateWorkflowHandle(..)


        #region CreateWorkflowRunHandle(..)

        /// <summary>
        /// Create an workflow run handle that represents a workflow run with the specified <c>workflowId</c> and <c>workflowRunId</c>.
        /// </summary>
        IWorkflowRun CreateWorkflowRunHandle(string workflowId,
                                             string workflowRunId);
        #endregion CreateWorkflowRunHandle(..)

        #endregion -- Workflow access and control APIs --


        #region -- Connection management --

        bool IsConnected { get; }

        /// <summary>
        /// <para>Ensure that the connection to the server is initialized and valid.</para>
        /// <para>The default implementation of this iface (<see cref="TemporalClient" />) has a factory method that created a client
        /// instace with a readily initialized connection (<see cref="TemporalClient.ConnectAsync" />). However, implementations
        /// of this iface may choose not to provide such a factory method. Users of such implementations can use this API to
        /// pro-actively initialize the server connection.<br .>
        /// This method must be a no-op, if the connection is already initialized.</para>
        /// <para>Implementations that use the Temporal server need to initialize the underlying connection by executing
        /// GetSystemInfo(..) to check the server health and get server capabilities. This API will explicitly perform that.
        /// If this API is not explicitly invoked by the user, implementations must ensure it is automaticlly invoked before placing
        /// any other calls.</para>
        /// </summary>
        /// <remarks>The default implementation of an <c>ITemporalClient</c> is <see cref="TemporalClient" />.
        /// It is recommended to use the factory method <see cref="TemporalClient.ConnectAsync" /> to create instanced of
        /// <c>TemporalClient</c>, and in such cases it is not required to explicitly call <c>EnsureConnectedAsync</c> on
        /// a new client instace (calling it will be a no-op).
        /// However, in some specific cases the user may NOT want to initialize the connection at client creation.
        /// For example, some clients require CancellationToken support. In some other scenarios, where the client is initialized by a
        /// dependency injection container, the user may not want to interact with the network until the dependency resolution is complete.
        /// In such cases, it is possible to create an instance of <c>TemporalClient</c> using the constructor. In such cases the client
        /// will automatically initialize its connection before it is used for the first time. However, in such scenarios, applications must
        /// be aware of the additional latency and possible errors which may occur during connection initialization.
        /// Invoking <c>EnsureConnectedAsync</c> will initialize the connection as a controled point in time where the user can
        /// control any such side-effects.
        /// </remarks>
        Task EnsureConnectedAsync(CancellationToken cancelToken = default);

        #endregion -- Connection management --
    }
}
