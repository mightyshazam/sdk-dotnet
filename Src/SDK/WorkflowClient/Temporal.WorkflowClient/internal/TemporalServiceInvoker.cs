using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Temporal.Util;
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

        public void Init(ITemporalClientInterceptor _)
        {
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        // Uncomment finalizer IFF `Dispose(bool disposing)` has code for freeing unmanaged resources.
        // ~TemporalServiceInvoker()
        // {
        //     Dispose(disposing: false);
        // }

        protected virtual void Dispose(bool disposing)
        {
            if (_payloadConverter is IDisposable disposablePayloadConverter)
            {
                disposablePayloadConverter.Dispose();
            }

            if (_payloadCodec != null && _payloadCodec is IDisposable disposablePayloadCodec)
            {
                disposablePayloadCodec.Dispose();
            }

            // @ToDo: handle _grpcServiceClient.
        }

        public async Task<StartWorkflow.Result> StartWorkflowAsync<TWfArg>(StartWorkflow.Arguments<TWfArg> opArgs)
        {
            // We need to re-validate the arguments because they went through the interceptor pipeline and thus may have
            // been modified by customer code.

            Validate.NotNull(opArgs);
            Validate.NotNullOrWhitespace(opArgs.Namespace);
            Validate.NotNullOrWhitespace(opArgs.WorkflowId);
            Validate.NotNullOrWhitespace(opArgs.WorkflowTypeName);
            Validate.NotNullOrWhitespace(opArgs.TaskQueue);
            Validate.NotNull(opArgs.WorkflowConfig);

            StartWorkflowExecutionRequest reqStartWf = new()
            {
                Namespace = opArgs.Namespace,
                WorkflowId = opArgs.WorkflowId,
                WorkflowType = new WorkflowType() { Name = opArgs.WorkflowTypeName },
                TaskQueue = new TaskQueue() { Name = opArgs.TaskQueue },

                Identity = opArgs.WorkflowConfig.Identity ?? _clientIdentityMarker,
                RequestId = Guid.NewGuid().ToString("D"),
            };

            Payloads serializedWfArg = new();
            PayloadConverter.Serialize(_payloadConverter, opArgs.WorkflowArg, serializedWfArg);

            if (_payloadCodec != null)
            {
                serializedWfArg = await _payloadCodec.EncodeAsync(serializedWfArg, opArgs.CancelToken);
            }

            if (opArgs.WorkflowConfig.WorkflowExecutionTimeout.HasValue)
            {
                reqStartWf.WorkflowExecutionTimeout = Duration.FromTimeSpan(opArgs.WorkflowConfig.WorkflowExecutionTimeout.Value);
            }

            if (opArgs.WorkflowConfig.WorkflowRunTimeout.HasValue)
            {
                reqStartWf.WorkflowRunTimeout = Duration.FromTimeSpan(opArgs.WorkflowConfig.WorkflowRunTimeout.Value);
            }

            if (opArgs.WorkflowConfig.WorkflowTaskTimeout.HasValue)
            {
                reqStartWf.WorkflowTaskTimeout = Duration.FromTimeSpan(opArgs.WorkflowConfig.WorkflowTaskTimeout.Value);
            }

            if (opArgs.WorkflowConfig.WorkflowIdReusePolicy.HasValue)
            {
                reqStartWf.WorkflowIdReusePolicy = opArgs.WorkflowConfig.WorkflowIdReusePolicy.Value;
            }

            if (opArgs.WorkflowConfig.RetryPolicy != null)
            {
                reqStartWf.RetryPolicy = opArgs.WorkflowConfig.RetryPolicy;
            }

            if (opArgs.WorkflowConfig.CronSchedule != null)
            {
                reqStartWf.CronSchedule = opArgs.WorkflowConfig.CronSchedule;
            }

            if (opArgs.WorkflowConfig.Memo != null)
            {
                throw new NotImplementedException("@ToDo");
            }

            if (opArgs.WorkflowConfig.SearchAttributes != null)
            {
                throw new NotImplementedException("@ToDo");
            }

            if (opArgs.WorkflowConfig.Header != null)
            {
                throw new NotImplementedException("@ToDo");
            }

            StatusCode rpcStatusCode = StatusCode.OK;

            StartWorkflowExecutionResponse resStartWf = await InvokeRemoteCallAndProcessErrors(
                    opArgs.Namespace,
                    opArgs.WorkflowId,
                    workflowRunId: null,
                    opArgs.CancelToken,
                    async (cancelCallToken) =>
                    {
                        try
                        {
                            return await _grpcServiceClient.StartWorkflowExecutionAsync(reqStartWf,
                                                                                        headers: null,
                                                                                        deadline: null,
                                                                                        cancelCallToken);
                        }
                        catch (RpcException rpcEx) when (rpcEx.StatusCode == StatusCode.AlreadyExists && !opArgs.ThrowOnAlreadyExists)
                        {
                            // Workflow already exists, but user specified not to throw in such cases => make a note and swallow exception.
                            // Other errors will be processed by invoker-wrapper.
                            rpcStatusCode = rpcEx.StatusCode;
                            return null;
                        }
                    });

            if (rpcStatusCode == StatusCode.OK)
            {
                return new StartWorkflow.Result(resStartWf.RunId);
            }
            else if (rpcStatusCode == StatusCode.AlreadyExists)
            {
                return new StartWorkflow.Result(rpcStatusCode);
            }
            else
            {
                throw new InvalidOperationException($"Unexpected {nameof(rpcStatusCode)}"
                                                  + $" ({rpcStatusCode.ToString()} = {((int) rpcStatusCode)})."
                                                  + $" Possible SDK bug. Please report.");
            }
        }

        [SuppressMessage("Style", "IDE0010:Add missing cases", Justification = "Switch on `historyEvent.EventType` only needs to process terminal events.")]
        public async Task<IWorkflowRunResult> AwaitConclusionAsync(AwaitConclusion.Arguments opArgs)
        {
            const string ServerCallDescriptionForDebug = nameof(_grpcServiceClient.GetWorkflowExecutionHistoryAsync)
                                                       + "(..) with HistoryEventFilterType = CloseEvent";
            const string ScenarioDescriptionForDebug = nameof(AwaitConclusionAsync);

            Validate.NotNull(opArgs);
            WorkflowRunHandle.ValidateWorkflowRunId(opArgs.WorkflowRunId);

            string workflowRunId = opArgs.WorkflowRunId;

            WorkflowRunResultFactory runResultFactory = new WorkflowRunResultFactory(_payloadConverter,
                                                                                     _payloadCodec,
                                                                                     opArgs.Namespace,
                                                                                     opArgs.WorkflowId,
                                                                                     opArgs.WorkflowChainId);

            ByteString nextPageToken = ByteString.Empty;

            // Spin and retry or follow the workflow chain until we get a result, or hit a non-retriable error, or time out:
            while (true)
            {
                opArgs.CancelToken.ThrowIfCancellationRequested();

                GetWorkflowExecutionHistoryRequest reqGetWfExHist = new()
                {
                    Namespace = opArgs.Namespace,
                    Execution = new WorkflowExecution()
                    {
                        WorkflowId = opArgs.WorkflowId,
                        RunId = workflowRunId ?? String.Empty,
                    },
                    NextPageToken = nextPageToken,
                    WaitNewEvent = true,
                    HistoryEventFilterType = HistoryEventFilterType.CloseEvent,
                };

                GetWorkflowExecutionHistoryResponse resGetWfExHist = await InvokeRemoteCallAndProcessErrors(
                        opArgs.Namespace,
                        opArgs.WorkflowId,
                        workflowRunId,
                        opArgs.CancelToken,
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

                        if (!String.IsNullOrWhiteSpace(nextRunId) && opArgs.FollowWorkflowChain)
                        {
                            workflowRunId = nextRunId;
                            continue;
                        }

                        return await runResultFactory.ForCompletedAsync(workflowRunId,
                                                                        historyEvent.WorkflowExecutionCompletedEventAttributes,
                                                                        opArgs.CancelToken);
                    }

                    case EventType.WorkflowExecutionFailed:
                    {
                        string nextRunId = historyEvent.WorkflowExecutionFailedEventAttributes.NewExecutionRunId;

                        if (!String.IsNullOrWhiteSpace(nextRunId) && opArgs.FollowWorkflowChain)
                        {
                            workflowRunId = nextRunId;
                            continue;
                        }

                        return await runResultFactory.ForFailedAsync(workflowRunId,
                                                                     historyEvent.WorkflowExecutionFailedEventAttributes,
                                                                     opArgs.CancelToken);
                    }

                    case EventType.WorkflowExecutionTimedOut:
                    {
                        string nextRunId = historyEvent.WorkflowExecutionTimedOutEventAttributes.NewExecutionRunId;

                        if (!String.IsNullOrWhiteSpace(nextRunId) && opArgs.FollowWorkflowChain)
                        {
                            workflowRunId = nextRunId;
                            continue;
                        }

                        return await runResultFactory.ForTimedOutAsync(workflowRunId,
                                                                       historyEvent.WorkflowExecutionTimedOutEventAttributes,
                                                                       opArgs.CancelToken);
                    }

                    case EventType.WorkflowExecutionCanceled:
                    {
                        return await runResultFactory.ForCanceledAsync(workflowRunId,
                                                                       historyEvent.WorkflowExecutionCanceledEventAttributes,
                                                                       opArgs.CancelToken);
                    }

                    case EventType.WorkflowExecutionTerminated:
                    {
                        return await runResultFactory.ForTerminatedAsync(workflowRunId,
                                                                         historyEvent.WorkflowExecutionTerminatedEventAttributes,
                                                                         opArgs.CancelToken);
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

                        if (opArgs.FollowWorkflowChain)
                        {
                            workflowRunId = nextRunId;
                            continue;
                        }

                        return await runResultFactory.ForContinuedAsNewAsync(workflowRunId,
                                                                             historyEvent.WorkflowExecutionContinuedAsNewEventAttributes,
                                                                             opArgs.CancelToken);
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

        public async Task<GetLatestWorkflowChainId.Result> GetLatestWorkflowChainIdAsync(GetLatestWorkflowChainId.Arguments opArgs)
        {
            const string ServerCallDescriptionForDebug = nameof(_grpcServiceClient.GetWorkflowExecutionHistoryAsync)
                                                       + "(..) with HistoryEventFilterType = AllEvent";
            const string ScenarioDescriptionForDebug = nameof(GetLatestWorkflowChainId);

            Validate.NotNull(opArgs);
            Validate.NotNullOrWhitespace(opArgs.Namespace);
            Validate.NotNullOrWhitespace(opArgs.WorkflowId);

            // Spin and retry or follow the workflow chain until we get a result, or hit a non-retriable error, or time out:
            while (true)
            {
                opArgs.CancelToken.ThrowIfCancellationRequested();

                GetWorkflowExecutionHistoryRequest reqGetWfExHist = new()
                {
                    Namespace = opArgs.Namespace,
                    Execution = new WorkflowExecution()
                    {
                        WorkflowId = opArgs.WorkflowId,
                        RunId = String.Empty,
                    },
                    NextPageToken = ByteString.Empty,
                    WaitNewEvent = false,
                    HistoryEventFilterType = HistoryEventFilterType.AllEvent,
                };

                GetWorkflowExecutionHistoryResponse resGetWfExHist = await InvokeRemoteCallAndProcessErrors(
                        opArgs.Namespace,
                        opArgs.WorkflowId,
                        workflowRunId: null,
                        opArgs.CancelToken,
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

                        return new GetLatestWorkflowChainId.Result(firstRunId);
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
