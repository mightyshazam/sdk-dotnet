using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Candidly.Util;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Temporal.Api.Common.V1;
using Temporal.Api.Enums.V1;
using Temporal.Api.History.V1;
using Temporal.Api.TaskQueue.V1;
using Temporal.Api.WorkflowService.V1;
using Temporal.Serialization;
using Temporal.WorkflowClient.Errors;
using Temporal.WorkflowClient.Interceptors;

namespace Temporal.WorkflowClient
{
    internal class TemporalServiceInvoker : ITemporalClientInterceptor
    {
        private readonly WorkflowService.WorkflowServiceClient _grpcServiceClient;
        private readonly string _clientIdentityMarker;
        private readonly IPayloadConverter _payloadConverter;
        private readonly IPayloadCodec _payloadCodec;

        public TemporalServiceInvoker(ChannelBase grpcChannel,
                                      string clientIdentityMarker,
                                      IPayloadConverter payloadConverter,
                                      IPayloadCodec payloadCodec)
        {
            Validate.NotNull(grpcChannel);
            Validate.NotNullOrWhitespace(clientIdentityMarker);
            Validate.NotNull(payloadConverter);
            // Note: payloadCodec may be null

            _grpcServiceClient = new WorkflowService.WorkflowServiceClient(grpcChannel);
            _clientIdentityMarker = clientIdentityMarker;
            _payloadConverter = payloadConverter;
            _payloadCodec = payloadCodec;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void Init(ITemporalClientInterceptor _)
        {
        }

        public async Task<StartWorkflowResult> StartWorkflowAsync<TWfArg>(string @namespace,
                                                                          string workflowId,
                                                                          string workflowTypeName,
                                                                          string taskQueue,
                                                                          TWfArg workflowArg,
                                                                          StartWorkflowChainConfiguration workflowConfig,
                                                                          bool throwOnAlreadyExists,
                                                                          CancellationToken cancelToken)
        {
            // We need to re-validate the arguments because they went through the interceptor pipeline and thus may have
            // been modified by customer code.

            Validate.NotNullOrWhitespace(@namespace);
            Validate.NotNullOrWhitespace(workflowId);
            Validate.NotNullOrWhitespace(workflowTypeName);
            Validate.NotNullOrWhitespace(taskQueue);
            Validate.NotNull(workflowConfig);

            StartWorkflowExecutionRequest reqStartWf = new()
            {
                Namespace = @namespace,
                WorkflowId = workflowId,
                WorkflowType = new WorkflowType() { Name = workflowTypeName },
                TaskQueue = new TaskQueue() { Name = taskQueue },

                Identity = workflowConfig.Identity ?? _clientIdentityMarker,
                RequestId = Guid.NewGuid().ToString("D"),
            };

            Payloads serializedWfArg = new();
            PayloadConverter.Serialize(_payloadConverter, workflowArg, serializedWfArg);

            if (_payloadCodec != null)
            {
                serializedWfArg = await _payloadCodec.EncodeAsync(serializedWfArg, cancelToken);
            }

            if (workflowConfig.WorkflowExecutionTimeout.HasValue)
            {
                reqStartWf.WorkflowExecutionTimeout = Duration.FromTimeSpan(workflowConfig.WorkflowExecutionTimeout.Value);
            }

            if (workflowConfig.WorkflowRunTimeout.HasValue)
            {
                reqStartWf.WorkflowRunTimeout = Duration.FromTimeSpan(workflowConfig.WorkflowRunTimeout.Value);
            }

            if (workflowConfig.WorkflowTaskTimeout.HasValue)
            {
                reqStartWf.WorkflowTaskTimeout = Duration.FromTimeSpan(workflowConfig.WorkflowTaskTimeout.Value);
            }

            if (workflowConfig.WorkflowIdReusePolicy.HasValue)
            {
                reqStartWf.WorkflowIdReusePolicy = workflowConfig.WorkflowIdReusePolicy.Value;
            }

            if (workflowConfig.RetryPolicy != null)
            {
                reqStartWf.RetryPolicy = workflowConfig.RetryPolicy;
            }

            if (workflowConfig.CronSchedule != null)
            {
                reqStartWf.CronSchedule = workflowConfig.CronSchedule;
            }

            if (workflowConfig.Memo != null)
            {
                throw new NotImplementedException("@ToDo");
            }

            if (workflowConfig.SearchAttributes != null)
            {
                throw new NotImplementedException("@ToDo");
            }

            if (workflowConfig.Header != null)
            {
                throw new NotImplementedException("@ToDo");
            }

            // Retry loop until exit condition met:
            while (true)
            {
                cancelToken.ThrowIfCancellationRequested();

                bool isAlreadyExists = false;

                StartWorkflowExecutionResponse resStartWf = await InvokeRemoteCallAndProcessErrors(
                        @namespace,
                        workflowId,
                        workflowRunId: null,
                        cancelToken,
                        async (cancelCallToken) =>
                        {
                            try
                            {
                                return await _grpcServiceClient.StartWorkflowExecutionAsync(reqStartWf,
                                                                                            headers: null,
                                                                                            deadline: null,
                                                                                            cancelCallToken);
                            }
                            catch (RpcException rpcEx) when (rpcEx.StatusCode == StatusCode.AlreadyExists && !throwOnAlreadyExists)
                            {
                                // Workflow already exists, but user specified not to throw in such cases => make a note and swallow exception.
                                // Other errors will be processed by invoker-wrapper.
                                isAlreadyExists = true;
                                return null;
                            }
                        });

                if (isAlreadyExists)
                {
                    return new StartWorkflowResult(null, StartWorkflowResult.Status.AlreadyExists);
                }

                if (resStartWf != null)
                {
                    return new StartWorkflowResult(resStartWf.RunId, StartWorkflowResult.Status.OK);
                }
            }
        }

        [SuppressMessage("Style", "IDE0010:Add missing cases", Justification = "Switch on `historyEvent.EventType` only needs to process terminal events.")]
        public async Task<IWorkflowRunResult> AwaitConclusionAsync(string @namespace,
                                                                   string workflowId,
                                                                   string workflowChainId,
                                                                   string workflowRunId,
                                                                   bool followChain,
                                                                   CancellationToken cancelToken)
        {
            const string ServerCallDescriptionForDebug = nameof(_grpcServiceClient.GetWorkflowExecutionHistoryAsync)
                                                       + "(..) with HistoryEventFilterType = CloseEvent";
            const string ScenarioDescriptionForDebug = nameof(AwaitConclusionAsync);

            WorkflowRun.ValidateWorkflowRunId(workflowRunId);

            WorkflowRunResultFactory runResultFactory = new WorkflowRunResultFactory(_payloadConverter,
                                                                                     _payloadCodec,
                                                                                     @namespace,
                                                                                     workflowId,
                                                                                     workflowChainId);

            ByteString nextPageToken = ByteString.Empty;

            // Spin and retry or follow the workflow chain until we get a result, or hit a non-retriable error, or time out:
            while (true)
            {
                cancelToken.ThrowIfCancellationRequested();

                GetWorkflowExecutionHistoryRequest reqGetWfExHist = new()
                {
                    Namespace = @namespace,
                    Execution = new WorkflowExecution()
                    {
                        WorkflowId = workflowId,
                        RunId = workflowRunId ?? String.Empty,
                    },
                    NextPageToken = nextPageToken,
                    WaitNewEvent = true,
                    HistoryEventFilterType = HistoryEventFilterType.CloseEvent,
                };

                GetWorkflowExecutionHistoryResponse resGetWfExHist = await InvokeRemoteCallAndProcessErrors(
                        @namespace,
                        workflowId,
                        workflowRunId,
                        cancelToken,
                        (cancelCallToken) => _grpcServiceClient.GetWorkflowExecutionHistoryAsync(reqGetWfExHist,
                                                                                                 headers: null,
                                                                                                 deadline: null,
                                                                                                 cancelCallToken));

                // IF we receive no history events AND a non-empty NextPageToken THEN Repeate the call:
                if (resGetWfExHist.History.Events.Count == 0 && resGetWfExHist.NextPageToken != null && resGetWfExHist.NextPageToken.Length > 0)
                {
                    nextPageToken = resGetWfExHist.NextPageToken;
                    continue;
                }

                if (resGetWfExHist.History.Events.Count != 1)
                {
                    throw new MalformedServerResponseException(ServerCallDescriptionForDebug,
                                                               ScenarioDescriptionForDebug,
                                                               $"History.Events.Count was expected to be 1, but it was"
                                                             + $" in fact {resGetWfExHist.History.Events.Count}");
                }

                HistoryEvent historyEvent = resGetWfExHist.History.Events[0];

                switch (historyEvent.EventType)
                {
                    case EventType.WorkflowExecutionCompleted:
                    {
                        string nextRunId = historyEvent.WorkflowExecutionFailedEventAttributes.NewExecutionRunId;

                        if (!String.IsNullOrWhiteSpace(nextRunId) && followChain)
                        {
                            workflowRunId = nextRunId;
                            continue;
                        }

                        return await runResultFactory.ForCompletedAsync(workflowRunId,
                                                                        historyEvent.WorkflowExecutionCompletedEventAttributes,
                                                                        cancelToken);
                    }

                    case EventType.WorkflowExecutionFailed:
                    {
                        string nextRunId = historyEvent.WorkflowExecutionFailedEventAttributes.NewExecutionRunId;

                        if (!String.IsNullOrWhiteSpace(nextRunId) && followChain)
                        {
                            workflowRunId = nextRunId;
                            continue;
                        }

                        return await runResultFactory.ForFailedAsync(workflowRunId,
                                                                     historyEvent.WorkflowExecutionFailedEventAttributes,
                                                                     cancelToken);
                    }

                    case EventType.WorkflowExecutionTimedOut:
                    {
                        string nextRunId = historyEvent.WorkflowExecutionTimedOutEventAttributes.NewExecutionRunId;

                        if (!String.IsNullOrWhiteSpace(nextRunId) && followChain)
                        {
                            workflowRunId = nextRunId;
                            continue;
                        }

                        return await runResultFactory.ForTimedOutAsync(workflowRunId,
                                                                       historyEvent.WorkflowExecutionTimedOutEventAttributes,
                                                                       cancelToken);
                    }

                    case EventType.WorkflowExecutionCanceled:
                    {
                        return await runResultFactory.ForCanceledAsync(workflowRunId,
                                                                       historyEvent.WorkflowExecutionCanceledEventAttributes,
                                                                       cancelToken);
                    }

                    case EventType.WorkflowExecutionTerminated:
                    {
                        return await runResultFactory.ForTerminatedAsync(workflowRunId,
                                                                         historyEvent.WorkflowExecutionTerminatedEventAttributes,
                                                                         cancelToken);
                    }

                    case EventType.WorkflowExecutionContinuedAsNew:
                    {
                        string nextRunId = historyEvent.WorkflowExecutionContinuedAsNewEventAttributes.NewExecutionRunId;

                        if (String.IsNullOrWhiteSpace(nextRunId))
                        {
                            throw new MalformedServerResponseException(ServerCallDescriptionForDebug,
                                                                       ScenarioDescriptionForDebug,
                                                                       $"EventType was WorkflowExecutionContinuedAsNew, but"
                                                                     + $" NewExecutionRunId={nextRunId.QuoteOrNull()}");
                        }

                        if (followChain)
                        {
                            workflowRunId = nextRunId;
                            continue;
                        }

                        return await runResultFactory.ForContinuedAsNewAsync(workflowRunId,
                                                                             historyEvent.WorkflowExecutionContinuedAsNewEventAttributes,
                                                                             cancelToken);
                    }

                    default:
                    {
                        throw new MalformedServerResponseException(ServerCallDescriptionForDebug,
                                                                   ScenarioDescriptionForDebug,
                                                                   $"Unexpected History EventType ({historyEvent.EventType})");
                    }
                }
            }  // while(true)
        }

        public async Task<string> GetLatestWorkflowChainId(string @namespace,
                                                           string workflowId,
                                                           CancellationToken cancelToken)
        {
            const string ServerCallDescriptionForDebug = nameof(_grpcServiceClient.GetWorkflowExecutionHistoryAsync)
                                                       + "(..) with HistoryEventFilterType = AllEvent";
            const string ScenarioDescriptionForDebug = nameof(GetLatestWorkflowChainId);

            Validate.NotNullOrWhitespace(@namespace);
            Validate.NotNullOrWhitespace(workflowId);

            // Spin and retry or follow the workflow chain until we get a result, or hit a non-retriable error, or time out:
            while (true)
            {
                cancelToken.ThrowIfCancellationRequested();

                GetWorkflowExecutionHistoryRequest reqGetWfExHist = new()
                {
                    Namespace = @namespace,
                    Execution = new WorkflowExecution()
                    {
                        WorkflowId = workflowId,
                        RunId = String.Empty,
                    },
                    NextPageToken = ByteString.Empty,
                    WaitNewEvent = false,
                    HistoryEventFilterType = HistoryEventFilterType.AllEvent,
                };

                GetWorkflowExecutionHistoryResponse resGetWfExHist = await InvokeRemoteCallAndProcessErrors(
                        @namespace,
                        workflowId,
                        workflowRunId: null,
                        cancelToken,
                        (cancelCallToken) => _grpcServiceClient.GetWorkflowExecutionHistoryAsync(reqGetWfExHist,
                                                                                                 headers: null,
                                                                                                 deadline: null,
                                                                                                 cancelCallToken));

                if (resGetWfExHist.History.Events.Count < 1)
                {
                    throw new MalformedServerResponseException(ServerCallDescriptionForDebug,
                                                               ScenarioDescriptionForDebug,
                                                               $"History.Events.Count was expected to be >= 1, but it was"
                                                             + $" in fact {resGetWfExHist.History.Events.Count}");
                }

                for (int e = 0; e < resGetWfExHist.History.Events.Count; e++)
                {
                    HistoryEvent historyEvent = resGetWfExHist.History.Events[e];
                    if (historyEvent.EventType == EventType.WorkflowExecutionStarted)
                    {
                        string firstRunId = historyEvent.WorkflowExecutionStartedEventAttributes.FirstExecutionRunId;

                        if (String.IsNullOrWhiteSpace(firstRunId))
                        {
                            throw new MalformedServerResponseException(ServerCallDescriptionForDebug,
                                                                       ScenarioDescriptionForDebug,
                                                                       $"WorkflowExecutionStartedEventAttributes.FirstExecutionRunId"
                                                                     + $" is {firstRunId.QuoteOrNull()}.");
                        }

                        return firstRunId;
                    }
                }

                throw new MalformedServerResponseException(ServerCallDescriptionForDebug,
                                                           ScenarioDescriptionForDebug,
                                                           $"No event with type `WorkflowExecutionStarted` found on the initial history page.");
            }  // while(true)
        }

        private static Task<TResponse> InvokeRemoteCallAndProcessErrors<TResponse>(string @namespace,
                                                                                   string workflowId,
                                                                                   string workflowRunId,
                                                                                   CancellationToken cancelToken,
                                                                                   Func<CancellationToken, AsyncUnaryCall<TResponse>> remoteCall)
                                                                        where TResponse : IMessage
        {
            return InvokeRemoteCallAndProcessErrors(@namespace,
                                                    workflowId,
                                                    workflowRunId,
                                                    cancelToken,
                                                    (ct) => remoteCall(ct).ResponseAsync);
        }

        private static async Task<TResponse> InvokeRemoteCallAndProcessErrors<TResponse>(string @namespace,
                                                                                         string workflowId,
                                                                                         string workflowRunId,
                                                                                         CancellationToken cancelToken,
                                                                                         Func<CancellationToken, Task<TResponse>> remoteCall)
                                                                        where TResponse : IMessage
        {
            try
            {
                return await remoteCall(cancelToken);
            }
            catch (OperationCanceledException ocEx) when (ocEx.CancellationToken == cancelToken)
            {
                // User triggered the specified cancelToken => just propagate cancellation.
                throw ocEx.Rethrow();
            }
            catch (RpcException rpcEx) when (rpcEx.StatusCode == StatusCode.AlreadyExists)
            {
                throw new WorkflowAlreadyExistsException(@namespace, workflowId, rpcEx);
            }
            catch (RpcException rpcEx) when (rpcEx.StatusCode == StatusCode.NotFound)
            {
                throw new WorkflowNotFoundException(@namespace, workflowId, workflowRunId, rpcEx);
            }
            catch (Exception ex)
            {
                // Future: Log if user logger available.
                throw new TemporalServiceException(@namespace, workflowId, workflowRunId, ex);
            }
        }
    }
}
