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

        public TemporalClient()
            : this(TemporalClientConfiguration.ForLocalHost())
        {
        }

        public TemporalClient(TemporalClientConfiguration config)
        {
            TemporalClientConfiguration.Validate(config);
            Configuration = config;
        }

        #endregion -- Static APIs --


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
            return StartWorkflowAsync<IDataValue.Void>(workflowId,
                                                       workflowTypeName,
                                                       taskQueue,
                                                       DataValue.Void,
                                                       workflowConfig,
                                                       cancelToken);
        }

        public Task<IWorkflowChain> StartWorkflowAsync<TWfArg>(string workflowId,
                                                                     string workflowTypeName,
                                                                     string taskQueue,
                                                                     TWfArg workflowArg,
                                                                     CancellationToken cancelToken = default)
        {
            Task<IWorkflowChain> startCompletion;

            if (workflowArg == null)
            {
                startCompletion = StartWorkflowAsync<IDataValue.Void>(workflowId,
                                                                      workflowTypeName,
                                                                      taskQueue,
                                                                      DataValue.Void,
                                                                      cancelToken);
            }

            if (workflowArg is IDataValue.Void voidData)
            {
                startCompletion = StartWorkflowAsync<IDataValue.Void>(workflowId,
                                                                      workflowTypeName,
                                                                      taskQueue,
                                                                      voidData,
                                                                      cancelToken);
            }
            else if (workflowArg is DataValue.INamedContainer namedCont)
            {
                startCompletion = StartWorkflowAsync<DataValue.INamedContainer>(workflowId,
                                                                                workflowTypeName,
                                                                                taskQueue,
                                                                                namedCont,
                                                                                cancelToken);
            }
            else if (workflowArg is DataValue.IUnnamedContainer unnamedCont)
            {
                startCompletion = StartWorkflowAsync<DataValue.IUnnamedContainer>(workflowId,
                                                                                  workflowTypeName,
                                                                                  taskQueue,
                                                                                  unnamedCont,
                                                                                  cancelToken);
            }
            else if (workflowArg is IDataValue dataValue)
            {
                startCompletion = StartWorkflowAsync<IDataValue>(workflowId,
                                                                 workflowTypeName,
                                                                 taskQueue,
                                                                 dataValue,
                                                                 cancelToken);
            }
            else if (workflowArg is System.Collections.IEnumerable)
            {
                throw CreateCannotUseEnumerableArgumentException(nameof(workflowArg), nameof(TWfArg), typeof(TWfArg).FullName);
            }
            else
            {
                startCompletion = StartWorkflowAsync<DataValue.UnnamedContainers.For1<TWfArg>>(workflowId,
                                                                                               workflowTypeName,
                                                                                               taskQueue,
                                                                                               DataValue.Unnamed<TWfArg>(workflowArg),
                                                                                               cancelToken);
            }

            return startCompletion;
        }

        public async Task<IWorkflowChain> StartWorkflowAsync<TWfArg>(string workflowId,
                                                                     string workflowTypeName,
                                                                     string taskQueue,
                                                                     TWfArg workflowArg,
                                                                     StartWorkflowChainConfiguration workflowConfig = null,
                                                                     CancellationToken cancelToken = default)
                                                            where TWfArg : IDataValue
        {
            IWorkflowChain workflow = CreateWorkflowHandle(workflowId);
            await workflow.StartAsync<TWfArg>(workflowTypeName,
                                              taskQueue,
                                              workflowArg,
                                              workflowConfig,
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
            return StartWorkflowWithSignalAsync<IDataValue.Void, IDataValue.Void>(workflowId,
                                                                                  workflowTypeName,
                                                                                  taskQueue,
                                                                                  DataValue.Void,
                                                                                  signalName,
                                                                                  DataValue.Void,
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
            return StartWorkflowWithSignalArgConvertAsync<IDataValue.Void, TSigArg>(workflowId,
                                                                                    workflowTypeName,
                                                                                    taskQueue,
                                                                                    DataValue.Void,
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
            return StartWorkflowWithSignalArgConvertAsync<TWfArg, IDataValue.Void>(workflowId,
                                                                                   workflowTypeName,
                                                                                   taskQueue,
                                                                                   workflowArg,
                                                                                   signalName,
                                                                                   DataValue.Void,
                                                                                   workflowConfig,
                                                                                   cancelToken);
        }

        public Task<IWorkflowChain> StartWorkflowWithSignalAsync<TWfArg, TSigArg>(string workflowId,
                                                                                  string workflowTypeName,
                                                                                  string taskQueue,
                                                                                  TWfArg workflowArg,
                                                                                  string signalName,
                                                                                  TSigArg signalArg,
                                                                                  CancellationToken cancelToken = default)
        {
            return StartWorkflowWithSignalArgConvertAsync<TWfArg, TSigArg>(workflowId,
                                                                           workflowTypeName,
                                                                           taskQueue,
                                                                           workflowArg,
                                                                           signalName,
                                                                           signalArg,
                                                                           workflowConfig: null,
                                                                           cancelToken);
        }

        private Task<IWorkflowChain> StartWorkflowWithSignalArgConvertAsync<TWfArg, TSigArg>(string workflowId,
                                                                                             string workflowTypeName,
                                                                                             string taskQueue,
                                                                                             TWfArg workflowArg,
                                                                                             string signalName,
                                                                                             TSigArg signalArg,
                                                                                             StartWorkflowChainConfiguration workflowConfig,
                                                                                             CancellationToken cancelToken)
        {
            if (workflowArg != null && workflowArg is System.Collections.IEnumerable)
            {
                throw CreateCannotUseEnumerableArgumentException(nameof(workflowArg), nameof(TWfArg), typeof(TWfArg).FullName);
            }

            if (signalArg != null && signalArg is System.Collections.IEnumerable)
            {
                throw CreateCannotUseEnumerableArgumentException(nameof(signalArg), nameof(TSigArg), typeof(TSigArg).FullName);
            }

            Task<IWorkflowChain> startCompletion;

            if (workflowArg == null || workflowArg is IDataValue.Void)
            {
                IDataValue.Void wfVoidArg = (workflowArg as IDataValue.Void) ?? DataValue.Void;

                if (signalArg == null || signalArg is IDataValue.Void)
                {
                    startCompletion = StartWorkflowWithSignalAsync<IDataValue.Void, IDataValue.Void>(
                                                                            workflowId,
                                                                            workflowTypeName,
                                                                            taskQueue,
                                                                            wfVoidArg,
                                                                            signalName,
                                                                            (signalArg as IDataValue.Void) ?? DataValue.Void,
                                                                            workflowConfig,
                                                                            cancelToken);
                }
                else if (signalArg is IDataValue sigDataValArg)
                {
                    startCompletion = StartWorkflowWithSignalAsync<IDataValue.Void, IDataValue>(
                                                                            workflowId,
                                                                            workflowTypeName,
                                                                            taskQueue,
                                                                            wfVoidArg,
                                                                            signalName,
                                                                            sigDataValArg,
                                                                            workflowConfig,
                                                                            cancelToken);
                }
                else
                {
                    startCompletion = StartWorkflowWithSignalAsync<IDataValue.Void, DataValue.UnnamedContainers.For1<TSigArg>>(
                                                                            workflowId,
                                                                            workflowTypeName,
                                                                            taskQueue,
                                                                            wfVoidArg,
                                                                            signalName,
                                                                            DataValue.Unnamed<TSigArg>(signalArg),
                                                                            workflowConfig,
                                                                            cancelToken);
                }
            }
            else if (workflowArg is IDataValue wfDataValArg)
            {
                if (signalArg == null || signalArg is IDataValue.Void)
                {
                    startCompletion = StartWorkflowWithSignalAsync<IDataValue, IDataValue.Void>(
                                                                            workflowId,
                                                                            workflowTypeName,
                                                                            taskQueue,
                                                                            wfDataValArg,
                                                                            signalName,
                                                                            (signalArg as IDataValue.Void) ?? DataValue.Void,
                                                                            workflowConfig,
                                                                            cancelToken);
                }
                else if (signalArg is IDataValue sigDataValArg)
                {
                    startCompletion = StartWorkflowWithSignalAsync<IDataValue, IDataValue>(
                                                                            workflowId,
                                                                            workflowTypeName,
                                                                            taskQueue,
                                                                            wfDataValArg,
                                                                            signalName,
                                                                            sigDataValArg,
                                                                            workflowConfig,
                                                                            cancelToken);
                }
                else
                {
                    startCompletion = StartWorkflowWithSignalAsync<IDataValue, DataValue.UnnamedContainers.For1<TSigArg>>(
                                                                            workflowId,
                                                                            workflowTypeName,
                                                                            taskQueue,
                                                                            wfDataValArg,
                                                                            signalName,
                                                                            DataValue.Unnamed<TSigArg>(signalArg),
                                                                            workflowConfig,
                                                                            cancelToken);
                }
            }
            else
            {
                DataValue.UnnamedContainers.For1<TWfArg> wfArgCont = DataValue.Unnamed<TWfArg>(workflowArg);
                if (signalArg == null || signalArg is IDataValue.Void)
                {
                    startCompletion = StartWorkflowWithSignalAsync<DataValue.UnnamedContainers.For1<TWfArg>, IDataValue.Void>(
                                                                            workflowId,
                                                                            workflowTypeName,
                                                                            taskQueue,
                                                                            wfArgCont,
                                                                            signalName,
                                                                            (signalArg as IDataValue.Void) ?? DataValue.Void,
                                                                            workflowConfig,
                                                                            cancelToken);
                }
                else if (signalArg is IDataValue sigDataValArg)
                {
                    startCompletion = StartWorkflowWithSignalAsync<DataValue.UnnamedContainers.For1<TWfArg>, IDataValue>(
                                                                            workflowId,
                                                                            workflowTypeName,
                                                                            taskQueue,
                                                                            wfArgCont,
                                                                            signalName,
                                                                            sigDataValArg,
                                                                            workflowConfig,
                                                                            cancelToken);
                }
                else
                {
                    startCompletion = StartWorkflowWithSignalAsync<DataValue.UnnamedContainers.For1<TWfArg>, DataValue.UnnamedContainers.For1<TSigArg>>(
                                                                            workflowId,
                                                                            workflowTypeName,
                                                                            taskQueue,
                                                                            wfArgCont,
                                                                            signalName,
                                                                            DataValue.Unnamed<TSigArg>(signalArg),
                                                                            workflowConfig,
                                                                            cancelToken);
                }
            }

            return startCompletion;
        }

        public async Task<IWorkflowChain> StartWorkflowWithSignalAsync<TWfArg, TSigArg>(string workflowId,
                                                                                        string workflowTypeName,
                                                                                        string taskQueue,
                                                                                        TWfArg workflowArg,
                                                                                        string signalName,
                                                                                        TSigArg signalArg,
                                                                                        StartWorkflowChainConfiguration workflowConfig,
                                                                                        CancellationToken cancelToken = default)
                                                            where TWfArg : IDataValue
                                                            where TSigArg : IDataValue
        {
            IWorkflowChain workflow = CreateWorkflowHandle(workflowId);
            await workflow.StartWithSignalAsync<TWfArg, TSigArg>(workflowTypeName,
                                                                 taskQueue,
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
            throw new NotImplementedException();
        }
        #endregion CreateWorkflowHandle(..)


        #region CreateWorkflowRunHandle(..)

        /// <summary>
        /// Create an workflow run handle that represents a workflow run with the specified <c>workflowId</c> and <c>workflowRunId</c>.
        /// </summary>
        public IWorkflowRun CreateWorkflowRunHandle(string workflowId,
                                                    string workflowRunId);
        #endregion CreateWorkflowRunHandle(..)

        #endregion -- Workflow access and control APIs --


        #region -- Connection management --
        public bool IsConectionInitialized { get; }

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
        public Task EnsureConnectionInitializedAsync(CancellationToken cancelToken = default);
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
