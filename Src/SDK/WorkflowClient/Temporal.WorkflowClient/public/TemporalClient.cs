using System;
using System.Threading;
using System.Threading.Tasks;
using Candidly.Util;
using Temporal.Common;

namespace Temporal.WorkflowClient
{
    public sealed class TemporalClient : ITemporalClient
    {
        #region -- Static APIs --

        /// <summary>
        /// </summary>
        /// <remarks></remarks>
        public static async Task<TemporalClient> ConnectAsync(TemporalClientConfiguration config)
        {
            TemporalClient client = new(config);
            await client.EnsureConnectionInitializedAsync(CancellationToken.None);
            return client;
        }

        #endregion -- Static APIs --

        public TemporalClient()
            : this(TemporalClientConfiguration.ForLocalHost())
        {
        }

        public TemporalClient(TemporalClientConfiguration config)
        {
            TemporalClientConfiguration.Validate(config);
            Configuration = config;
            IsConectionInitialized = false;
        }

        #region -- Common properties --

        public TemporalClientConfiguration Configuration { get; init; }

        public string Namespace { get { return Configuration.Namespace; } }

        #endregion -- Common properties --


        #region -- Workflow access and control APIs --

        #region StartWorkflowAsync(..)

        public Task<IWorkflowChain> StartWorkflowAsync(string workflowId,
                                                       string workflowTypeName,
                                                       string taskQueue,
                                                       StartWorkflowChainConfiguration workflowConfig = null,
                                                       CancellationToken cancelToken = default)
        {
            return StartWorkflowAsync<IDataValue.Void>(workflowId, workflowTypeName, taskQueue, DataValue.Void, workflowConfig, cancelToken);
        }

        public async Task<IWorkflowChain> StartWorkflowAsync<TWfArg>(string workflowId,
                                                                     string workflowTypeName,
                                                                     string taskQueue,
                                                                     TWfArg workflowArg,
                                                                     StartWorkflowChainConfiguration workflowConfig = null,
                                                                     CancellationToken cancelToken = default)
        {
            IWorkflowChain workflow = CreateWorkflowHandle(workflowId);
            await workflow.StartAsync<TWfArg>(workflowTypeName, taskQueue, workflowArg, workflowConfig, cancelToken);
            return workflow;
        }

        #endregion StartWorkflowAsync(..)


        #region StartWorkflowWithSignalAsync(..)

        public Task<IWorkflowChain> StartWorkflowWithSignalAsync(string workflowId,
                                                                 string workflowTypeName,
                                                                 string taskQueue,
                                                                 string signalName,
                                                                 StartWorkflowChainConfiguration workflowConfig = null,
                                                                 CancellationToken cancelToken = default)
        {
            return StartWorkflowWithSignalAsync<IDataValue.Void, IDataValue.Void>(workflowId,
                                                                                  workflowTypeName,
                                                                                  taskQueue,
                                                                                  workflowArg: DataValue.Void,
                                                                                  signalName,
                                                                                  signalArg: DataValue.Void,
                                                                                  workflowConfig,
                                                                                  cancelToken);
        }

        public Task<IWorkflowChain> StartWorkflowWithSignalAsync<TSigArg>(string workflowId,
                                                                          string workflowTypeName,
                                                                          string taskQueue,
                                                                          string signalName,
                                                                          TSigArg signalArg,
                                                                          StartWorkflowChainConfiguration workflowConfig = null,
                                                                          CancellationToken cancelToken = default)
        {
            return StartWorkflowWithSignalAsync<IDataValue.Void, TSigArg>(workflowId,
                                                                          workflowTypeName,
                                                                          taskQueue,
                                                                          workflowArg: DataValue.Void,
                                                                          signalName,
                                                                          signalArg,
                                                                          workflowConfig,
                                                                          cancelToken);
        }

        public Task<IWorkflowChain> StartWorkflowWithSignalAsync<TWfArg>(string workflowId,
                                                                         string workflowTypeName,
                                                                         string taskQueue,
                                                                         TWfArg workflowArg,
                                                                         string signalName,
                                                                         StartWorkflowChainConfiguration workflowConfig = null,
                                                                         CancellationToken cancelToken = default)
        {
            return StartWorkflowWithSignalAsync<TWfArg, IDataValue.Void>(workflowId,
                                                                         workflowTypeName,
                                                                         taskQueue,
                                                                         workflowArg,
                                                                         signalName,
                                                                         signalArg: DataValue.Void,
                                                                         workflowConfig,
                                                                         cancelToken);
        }

        public async Task<IWorkflowChain> StartWorkflowWithSignalAsync<TWfArg, TSigArg>(string workflowId,
                                                                                        string workflowTypeName,
                                                                                        string taskQueue,
                                                                                        TWfArg workflowArg,
                                                                                        string signalName,
                                                                                        TSigArg signalArg,
                                                                                        StartWorkflowChainConfiguration workflowConfig = null,
                                                                                        CancellationToken cancelToken = default)
        {
            IWorkflowChain workflow = CreateWorkflowHandle(workflowId);
            await workflow.StartWithSignalAsync<TWfArg, TSigArg>(workflowTypeName, taskQueue,
                                                                 workflowArg,
                                                                 signalName,
                                                                 signalArg,
                                                                 workflowConfig,
                                                                 cancelToken);
            return workflow;
        }
        #endregion StartWorkflowWithSignalAsync(..)


        #region CreateWorkflowHandle(..)        

        /// <summary>
        /// Create an unbound workflow chain handle.
        /// The handle will be bound to the most recent chain with the specified <c>workflowId</c> once the user invokes an API
        /// the end up interacting with some workflow run on the server.
        /// </summary>
        public IWorkflowChain CreateWorkflowHandle(string workflowId)
        {
            return new WorkflowChain(this, workflowId);
        }

        /// <summary>
        /// Create an workflow chain handle that represents a workflow chain with the specified <c>workflowId</c> and <c>workflowChainId</c>.
        /// </summary>
        /// <param name="workflowId">The workflow-id of the workflow chain that will be represented by the newly created handle.</param>
        /// <param name="workflowChainId">The workflow-run-id of the <em>first workflow run</em> of the <em>workflow chain</em> represented
        /// by the newly created handle.</param>
        public IWorkflowChain CreateWorkflowHandle(string workflowId,
                                                   string workflowChainId)
        {
            throw new NotImplementedException("@ToDo");
        }
        #endregion CreateWorkflowHandle(..)


        #region CreateWorkflowRunHandle(..)

        /// <summary>
        /// Create an workflow run handle that represents a workflow run with the specified <c>workflowId</c> and <c>workflowRunId</c>.
        /// </summary>
        public IWorkflowRun CreateWorkflowRunHandle(string workflowId,
                                                    string workflowRunId)
        {
            throw new NotImplementedException("@ToDo");
        }
        #endregion CreateWorkflowRunHandle(..)

        #endregion -- Workflow access and control APIs --


        #region -- Connection management --
        public bool IsConectionInitialized { get; private set; }

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
        /// <c>TemporalClient</c>, and in such cases it is not required to explicitly call <c>EnsureConnectionInitializedAsync</c> on
        /// a new client instace (calling it will be a no-op).
        /// However, in some specific cases the user may NOT want to initialize the connection at client creation.
        /// For example, some clients require CancellationToken support. In some other scenarios, where the client is initialized by a
        /// dependency injection container, the user may not want to interact with the network until the dependency resolution is complete.
        /// In such cases, it is possible to create an instance of <c>TemporalClient</c> using the constructor. In such cases the client
        /// will automatically initialize its connection before it is used for the first time. However, in such scenarios, applications must
        /// be aware of the additional latency and possible errors which may occur during connection initialization.
        /// Invoking <c>EnsureConnectionInitializedAsync</c> will initialize the connection as a controled point in time where the user can
        /// control any such side-effects.
        /// </remarks>
        public Task EnsureConnectionInitializedAsync(CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }
        #endregion -- Connection management --

        #region Privates
        private static ArgumentException CreateCannotUseEnumerableArgumentException(string argParamName, string typeParamName, string typeParamType)
        {
            throw new ArgumentException($"The specified {argParamName} is an IEnumerable (and the type argument"
                                      + $" {typeParamName} is \"{typeParamType}\"). Specifying Enumerables (arrays,"
                                      + $" collections, ...) as {argParamName} is not permitted because it is not clear"
                                      + $" whether you intended to pass the Enumerable as the single argument, or whether you"
                                      + $" intended to pass multiple arguments, one for each element of your collection."
                                      + $" To pass an Enumerable as a single argument, or to pass several arguments to a workflow,"
                                      + $" you need to wrap your data into an IDavaValue container. For example, to pass an array"
                                      + $" of integers (`int[] data`) as a single argument, you can wrap it like this:"
                                      + $" `{nameof(StartWorkflowAsync)}(.., {nameof(DataValue)}.{nameof(DataValue.Unnamed)}<int[]>(data))`."
                                      + $" To pass the contents of `data` as multiple integer arguments, wrap it like this:"
                                      + $" `{nameof(StartWorkflowAsync)}(.., {nameof(DataValue)}.{nameof(DataValue.Unnamed)}<int>(data))`."
                                      + $" Also, if suported by the workflow implementation, it is better to use a"
                                      + $" {nameof(DataValue.Named)} {nameof(DataValue)}-container.");
        }
        #endregion Privates
    }
}
