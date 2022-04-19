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
            ValidateWorkflowProperty.WorkflowId(opArgs.WorkflowId);
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
            Validate.NotNullOrWhitespace(opArgs.Namespace);
            ValidateWorkflowProperty.RunId.SpecifiedOrUnspecified(opArgs.WorkflowRunId);

            string workflowRunId = opArgs.WorkflowRunId;
            string workflowChainId = opArgs.WorkflowChainId;

            if (workflowChainId == null)
            {
                // Temporary workaround for missing server features. See comments in the invoked method for more info.
                HackyWorkflowChainBindingInfo bindingInfo = await GetBindingInfoTemporaryHackAsync(opArgs.Namespace,
                                                                                                   opArgs.WorkflowId,
                                                                                                   workflowRunId,
                                                                                                   opArgs.CancelToken);
                if (bindingInfo.IsSuccess)
                {
                    workflowRunId = bindingInfo.WorkflowRunId;
                    workflowChainId = bindingInfo.WorkflowChainId;
                }
            }

            WorkflowRunResultFactory runResultFactory = new WorkflowRunResultFactory(_payloadConverter,
                                                                                     _payloadCodec,
                                                                                     opArgs.Namespace,
                                                                                     opArgs.WorkflowId,
                                                                                     workflowChainId);

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

        public async Task<GetWorkflowChainId.Result> GetWorkflowChainIdAsync(GetWorkflowChainId.Arguments opArgs)
        {
            const string ServerCallDescriptionForDebug = nameof(_grpcServiceClient.GetWorkflowExecutionHistoryAsync)
                                                       + "(..) with HistoryEventFilterType = AllEvent";
            const string ScenarioDescriptionForDebug = nameof(GetWorkflowChainIdAsync);

            Validate.NotNull(opArgs);
            Validate.NotNullOrWhitespace(opArgs.Namespace);
            ValidateWorkflowProperty.WorkflowId(opArgs.WorkflowId);
            ValidateWorkflowProperty.RunId.SpecifiedOrUnspecified(opArgs.WorkflowRunId);

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
                        RunId = opArgs.WorkflowRunId ?? String.Empty,
                    },
                    NextPageToken = ByteString.Empty,
                    WaitNewEvent = false,
                    HistoryEventFilterType = HistoryEventFilterType.AllEvent,
                };

                GetWorkflowExecutionHistoryResponse resGetWfExHist = await InvokeRemoteCallAndProcessErrors(
                        opArgs.Namespace,
                        opArgs.WorkflowId,
                        opArgs.WorkflowRunId,
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

                        return new GetWorkflowChainId.Result(firstRunId);
                    }
                }

                throw new MalformedServerResponseException(ServerCallDescriptionForDebug,
                                                           ScenarioDescriptionForDebug,
                                                           $"No event with type `WorkflowExecutionStarted` found on the initial history page.");
            }  // while(true)
        }

        public async Task<DescribeWorkflowRun.Result> DescribeWorkflowRunAsync(DescribeWorkflowRun.Arguments opArgs)
        {
            Validate.NotNull(opArgs);
            Validate.NotNullOrWhitespace(opArgs.Namespace);
            ValidateWorkflowProperty.WorkflowId(opArgs.WorkflowId);
            ValidateWorkflowProperty.ChainId.BoundOrUnbound(opArgs.WorkflowChainId);
            ValidateWorkflowProperty.RunId.SpecifiedOrUnspecified(opArgs.WorkflowRunId);

            string workflowRunId = opArgs.WorkflowRunId;
            string workflowChainId = opArgs.WorkflowChainId;

            // Do not call the hack for a null workflowRunId to prevent infinite recursion.
            // Instead, if noth runId and chainId are null, descrive the very latest run of all chains and then
            // use the runId obtained by doing that to fill in the chainId later.
            if (workflowRunId != null && workflowChainId == null)
            {
                // Temporary workaround for missing server features. See comments in the invoked method for more info.
                HackyWorkflowChainBindingInfo bindingInfo = await GetBindingInfoTemporaryHackAsync(opArgs.Namespace,
                                                                                                   opArgs.WorkflowId,
                                                                                                   workflowRunId,
                                                                                                   opArgs.CancelToken);
                if (bindingInfo.IsSuccess)
                {
                    workflowRunId = bindingInfo.WorkflowRunId;
                    workflowChainId = bindingInfo.WorkflowChainId;
                }
            }

            // Spin and retry or follow the workflow chain until we get a result, or hit a non-retriable error, or time out:
            while (true)
            {
                opArgs.CancelToken.ThrowIfCancellationRequested();

                DescribeWorkflowExecutionRequest reqDescrWfExec = new()
                {
                    Namespace = opArgs.Namespace,
                    Execution = new WorkflowExecution()
                    {
                        WorkflowId = opArgs.WorkflowId,
                        RunId = opArgs.WorkflowRunId ?? String.Empty,
                    }
                };

                StatusCode rpcStatusCode = StatusCode.OK;

                DescribeWorkflowExecutionResponse resDescrWfExec = await InvokeRemoteCallAndProcessErrors(
                        opArgs.Namespace,
                        opArgs.WorkflowId,
                        opArgs.WorkflowRunId,
                        opArgs.CancelToken,
                        async (cancelCallToken) =>
                        {
                            try
                            {
                                return await _grpcServiceClient.DescribeWorkflowExecutionAsync(reqDescrWfExec,
                                                                                               headers: null,
                                                                                               deadline: null,
                                                                                               cancelCallToken);
                            }
                            catch (RpcException rpcEx) when (rpcEx.StatusCode == StatusCode.NotFound && !opArgs.ThrowIfWorkflowNotFound)
                            {
                                // Workflow not found, but user specified not to throw in such cases => make a note and swallow exception.
                                // Other errors will be processed by invoker-wrapper.
                                rpcStatusCode = rpcEx.StatusCode;
                                return null;
                            }
                        });

                if (rpcStatusCode == StatusCode.OK)
                {
                    workflowRunId = resDescrWfExec.WorkflowExecutionInfo.Execution.RunId;

                    // If we did not apply the temnporary binding hack earlier, do it now.
                    if (workflowRunId != null && workflowChainId == null)
                    {
                        // Temporary workaround for missing server features. See comments in the invoked method for more info.
                        HackyWorkflowChainBindingInfo bindingInfo = await GetBindingInfoTemporaryHackAsync(opArgs.Namespace,
                                                                                                           opArgs.WorkflowId,
                                                                                                           workflowRunId,
                                                                                                           opArgs.CancelToken);
                        if (bindingInfo.IsSuccess)
                        {
                            workflowRunId = bindingInfo.WorkflowRunId;
                            workflowChainId = bindingInfo.WorkflowChainId;
                        }
                    }

                    return new DescribeWorkflowRun.Result(resDescrWfExec, workflowChainId);
                }
                else if (rpcStatusCode == StatusCode.NotFound)
                {
                    return new DescribeWorkflowRun.Result(rpcStatusCode);
                }
                else
                {
                    throw new InvalidOperationException($"Unexpected {nameof(rpcStatusCode)}"
                                                      + $" ({rpcStatusCode.ToString()} = {((int) rpcStatusCode)})."
                                                      + $" Possible SDK bug. Please report on: https://github.com/temporalio/sdk-dotnet/issues");
                }
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

        /// <summary>
        /// Many server APIs optonally take a null workflow-run-id to refer to the latest run/chain for the given workflow-run-id.
        /// In the long-term, we will make such APIs return the workflow-chain-id that was chosen (aka the first-run-of-the-chain-id).
        /// Once that is done, and we will bind this chain to that ID.
        /// Details in the issue tracker: https://github.com/temporalio/temporal/issues/2691
        /// !! At that time we must remove this method and all calls to it !!
        /// !! https://github.com/temporalio/sdk-dotnet/issues/29 !!
        /// Until then we use this method to ensure in the same observable behaviour at the cost of one additional remote call
        /// before the first remote call the chain makes.
        /// Note that since some server APIs do not even take a chain id (aka first run) parameter, there is still a racy
        /// behaviour difference and chain handle can "pverflow". We still use this hack to simulate the "binding".
        /// </summary>
        private async Task<HackyWorkflowChainBindingInfo> GetBindingInfoTemporaryHackAsync(string @namespace,
                                                                                           string workflowId,
                                                                                           string workflowRunId,
                                                                                           CancellationToken cancelToken)
        {
            if (workflowRunId == null)
            {
                DescribeWorkflowRun.Result resDescrWfExec = await DescribeWorkflowRunAsync(
                                                                        new DescribeWorkflowRun.Arguments(@namespace,
                                                                                                          workflowId,
                                                                                                          WorkflowChainId: null,
                                                                                                          WorkflowRunId: null,
                                                                                                          ThrowIfWorkflowNotFound: false,
                                                                                                          cancelToken));
                if (resDescrWfExec.StatusCode != StatusCode.OK)
                {
                    return new HackyWorkflowChainBindingInfo(false, null, null);
                }

                workflowRunId = resDescrWfExec.DescribeWorkflowExecutionResponse.WorkflowExecutionInfo.Execution.RunId;
            }

            GetWorkflowChainId.Result resGetWfChainId = await GetWorkflowChainIdAsync(
                                                                        new GetWorkflowChainId.Arguments(@namespace,
                                                                                                         workflowId,
                                                                                                         workflowRunId,
                                                                                                         cancelToken));
            return new HackyWorkflowChainBindingInfo(true, resGetWfChainId.WorkflowChainId, workflowRunId);
        }

        private record HackyWorkflowChainBindingInfo(bool IsSuccess, string WorkflowChainId, string WorkflowRunId);
    }
}
