using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Candidly.Util;
using Grpc.Core;
using Temporal.Common;
using Temporal.Serialization;
using Temporal.WorkflowClient.Interceptors;

namespace Temporal.WorkflowClient
{
    public sealed class TemporalClient : ITemporalClient
    {
        #region -- Static APIs --

        private static int s_identityMarkersCount = 0;

        /// <summary>
        /// </summary>
        /// <remarks></remarks>
        public static async Task<ITemporalClient> ConnectAsync(TemporalClientConfiguration config, CancellationToken cancelToken = default)
        {
            TemporalClient client = new(config);
            await client.EnsureConnectedAsync(cancelToken);
            return client;
        }

        private static string CreateIdentityMarker()
        {
            int identityMarkersIndex;
            unchecked
            {
                identityMarkersIndex = Interlocked.Increment(ref s_identityMarkersCount);
            }

            try
            {
                CurrentProcess.GetIdentityInfo(out string processName, out string machineName, out int processId);
                return $"{machineName}/{processName}/{processId}/{identityMarkersIndex}";
            }
            catch
            {
                return $"{Guid.NewGuid().ToString("D")}/{identityMarkersIndex}";
            }
        }

        #endregion -- Static APIs --


        #region -- Fields, Ctors, Common properties --

        private readonly ChannelBase _grpcChannel;
        private readonly string _identityMarker;

        public TemporalClient()
            : this(TemporalClientConfiguration.ForLocalHost())
        {
        }

        public TemporalClient(TemporalClientConfiguration config)
        {
            TemporalClientConfiguration.Validate(config);

            Configuration = config;

            _grpcChannel = GrpcChannelFactory.SingletonInstance.GetOrCreateChannel(config);
            _identityMarker = CreateIdentityMarker();
        }

        public TemporalClientConfiguration Configuration { get; }

        public string Namespace { get { return Configuration.Namespace; } }

        #endregion -- Fields, Ctors, Common properties --


        #region -- Workflow access and control APIs --

        #region StartWorkflowAsync(..)

        public Task<IWorkflowChain> StartWorkflowAsync(string workflowId,
                                                       string workflowTypeName,
                                                       string taskQueue,
                                                       StartWorkflowChainConfiguration workflowConfig = null,
                                                       CancellationToken cancelToken = default)
        {
            return StartWorkflowAsync<IPayload.Void>(workflowId, workflowTypeName, taskQueue, Payload.Void, workflowConfig, cancelToken);
        }

        public async Task<IWorkflowChain> StartWorkflowAsync<TWfArg>(string workflowId,
                                                                     string workflowTypeName,
                                                                     string taskQueue,
                                                                     TWfArg workflowArg,
                                                                     StartWorkflowChainConfiguration workflowConfig = null,
                                                                     CancellationToken cancelToken = default)
        {
            IWorkflowChain workflow = CreateWorkflowHandle(workflowId);
            await workflow.StartAsync<TWfArg>(workflowTypeName,
                                              taskQueue,
                                              workflowArg,
                                              workflowConfig,
                                              throwIfWorkflowChainAlreadyExists: true,
                                              cancelToken);
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
            return StartWorkflowWithSignalAsync<IPayload.Void, IPayload.Void>(workflowId,
                                                                              workflowTypeName,
                                                                              taskQueue,
                                                                              workflowArg: Payload.Void,
                                                                              signalName,
                                                                              signalArg: Payload.Void,
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
            return StartWorkflowWithSignalAsync<IPayload.Void, TSigArg>(workflowId,
                                                                        workflowTypeName,
                                                                        taskQueue,
                                                                        workflowArg: Payload.Void,
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
            return StartWorkflowWithSignalAsync<TWfArg, IPayload.Void>(workflowId,
                                                                       workflowTypeName,
                                                                       taskQueue,
                                                                       workflowArg,
                                                                       signalName,
                                                                       signalArg: Payload.Void,
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

        public bool IsConnected { get; private set; }

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
        public Task EnsureConnectedAsync(CancellationToken cancelToken = default)
        {
            if (IsConnected)
            {
                return Task.CompletedTask;
            }

            return ConnectAndValidateAsync(cancelToken);
        }

        private Task ConnectAndValidateAsync(CancellationToken cancelToken)
        {
            // @ToDo: Call server to get capabilities

            cancelToken.ThrowIfCancellationRequested();
            IsConnected = true;
            return Task.CompletedTask;
        }

        #endregion -- Connection management --


        #region -- Service invocation pipeline management --

        /// <summary>
        /// <c>WorkflowChain</c> instances call this to create the invocation pipelie for themselves.
        /// During races, this mthod may be called several times for a given chain, but only the pipeline returned
        /// by the first completing invocation will atomically set, others will be discarded.
        /// </summary>        
        internal ITemporalClientInterceptor CreateServiceInvocationPipeline(IWorkflowChain workflowHandle)
        {
            // Create default interceptor pipelie for all workflows (tracing etc..):

            List<ITemporalClientInterceptor> pipeline = CreateDefaultServiceInvocationPipeline(workflowHandle);

            // Apply custom interceptor factory:

            Action<IWorkflowChain, IList<ITemporalClientInterceptor>> customInterceptorFactory = Configuration.ClientInterceptorFactory;
            if (customInterceptorFactory != null)
            {
                customInterceptorFactory(workflowHandle, pipeline);
            }

            // Now we need to add the final interceptor, aka the "sink".

            // Create the payload converter for the sink:

            IPayloadConverter payloadConverter = null;
            Func<IWorkflowChain, IPayloadConverter> customPayloadConverterFactory = Configuration.PayloadConverterFactory;
            if (customPayloadConverterFactory != null)
            {
                payloadConverter = customPayloadConverterFactory(workflowHandle);
            }

            if (payloadConverter == null)
            {
                payloadConverter = new AggregatePayloadConverter();
            }

            // Create the payload codec for the sink:

            IPayloadCodec payloadCodec = null;
            Func<IWorkflowChain, IPayloadCodec> customPayloadCodecFactory = Configuration.PayloadCodecFactory;
            if (customPayloadCodecFactory != null)
            {
                payloadCodec = customPayloadCodecFactory(workflowHandle);
            }

            // Create the sink:

            ITemporalClientInterceptor downstream = new TemporalServiceInvoker(_grpcChannel,
                                                                               _identityMarker,
                                                                               payloadConverter,
                                                                               payloadCodec);

            // Build the pipeline by creating the chain of interceptors ending with the sink:

            downstream.Init(nextInterceptor: null);

            for (int i = pipeline.Count - 1; i >= 0; i--)
            {
                ITemporalClientInterceptor current = pipeline[i];
                if (current != null)
                {
                    current.Init(downstream);
                    downstream = current;
                }
            }

            return downstream;
        }

        private List<ITemporalClientInterceptor> CreateDefaultServiceInvocationPipeline(IWorkflowChain _)
        {
            List<ITemporalClientInterceptor> pipeline = new List<ITemporalClientInterceptor>();
            return pipeline;
        }

        #endregion -- Service invocation pipeline management --
    }
}
