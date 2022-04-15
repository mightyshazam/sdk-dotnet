using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Candidly.Util;
using Temporal.Api.Common.V1;
using Temporal.Api.Failure.V1;
using Temporal.Common.Payloads;
using Temporal.Serialization;

namespace Temporal.WorkflowClient.Errors
{
    public static class TemporalFailure
    {
        private static class StackTraceMarkers
        {
            public const string StartRemoteTracePrefix = "----------- Start of remote stack trace";
            public const string StartRemoteTraceTemplate = StartRemoteTracePrefix + " ('{0}' based on '{1}' from '{2}') -----------";

            public const string EndRemoteTracePrefix = "----------- End of remote stack trace";
            public const string EndRemoteTraceTemplate = EndRemoteTracePrefix + " ('{0}' based on '{1}' from '{2}') -----------";

            public const string UnknownSourceMoniker = "unknown source";
        }

        public static async Task<Exception> FromMessageAsync(Failure failure,
                                                             IPayloadConverter payloadConverter,
                                                             IPayloadCodec payloadCodec,
                                                             CancellationToken cancelToken)
        {
            Validate.NotNull(failure);

            if (failure.Cause == null)
            {
                return await FromMessageWithCauseAsync(failure, cause: null, causeChainDepth: 0, payloadConverter, payloadCodec, cancelToken);
            }

            // Don't joke with how deeply nested this can be.
            // Unwind the cause chain so that we can rehydrate them in a non-recursive way.

            List<Failure> failures = new();
            Failure nextFailure = failure;
            while (nextFailure != null)
            {
                failures.Add(nextFailure);
                nextFailure = nextFailure.Cause;
            }

            // Now build the chain starting from the most inner cause:

            Exception failureException = null;
            for (int f = failures.Count - 1; f >= 0; f--)
            {
                failureException = await FromMessageWithCauseAsync(failures[f], failureException, f, payloadConverter, payloadCodec, cancelToken);
            }

            return failureException;
        }

        private static async Task<Exception> FromMessageWithCauseAsync(Failure failure,
                                                                       Exception cause,
                                                                       int causeChainDepth,
                                                                       IPayloadConverter payloadConverter,
                                                                       IPayloadCodec payloadCodec,
                                                                       CancellationToken cancelToken)
        {
            GetNetExceptionTypeInfo(failure,
                                    out Type exceptionType,
                                    out Func<Failure,
                                             SerializationInfo,
                                             IPayloadConverter,
                                             IPayloadCodec,
                                             CancellationToken,
                                             Task<Exception>> exceptionFactory);

            SerializationInfo info = new(exceptionType, new FormatterConverter());

            // This is a joint set of values set by Net Fx 4.6.x and Net 6.
            // Values not needed in all versions (e.g. Net 6 does not use class name) should be ignired during rehydration.

            // Populate the basics:

            string failureSource = failure.Source?.Trim();

            info.AddValue("ClassName", exceptionType.ToString(), typeof(string));
            info.AddValue("Message", failure.Message?.Trim() ?? String.Empty, typeof(string));
            info.AddValue("Data", null, typeof(System.Collections.IDictionary));
            info.AddValue("InnerException", cause, typeof(Exception));
            info.AddValue("HelpURL", null, typeof(string));
            info.AddValue("StackTraceString", null, typeof(string));
            info.AddValue("RemoteStackIndex", causeChainDepth, typeof(int));
            info.AddValue("ExceptionMethod", String.Empty, typeof(string));  // Future: attempt to populate this
            info.AddValue("HResult", unchecked((int) HResult.COR_E_EXCEPTION));  // Future: can we be more specific?
            info.AddValue("Source", failureSource ?? String.Empty, typeof(string));
            info.AddValue("WatsonBuckets", null, typeof(byte[]));

            // Populate the remote stack trace:

            if (String.IsNullOrWhiteSpace(failure.StackTrace))
            {
                info.AddValue("RemoteStackTraceString", null, typeof(string));
            }
            else
            {
                StringBuilder remoteTraceBuilder = new();

                bool alreadyMarkedUp = failure.StackTrace.IndexOf(StackTraceMarkers.StartRemoteTracePrefix, StringComparison.OrdinalIgnoreCase) != -1
                                            || failure.StackTrace.IndexOf(StackTraceMarkers.EndRemoteTracePrefix, StringComparison.OrdinalIgnoreCase) != -1;

                if (!alreadyMarkedUp)
                {
                    remoteTraceBuilder.AppendFormat(StackTraceMarkers.StartRemoteTraceTemplate,
                                                    exceptionType.Name,
                                                    failure.FailureInfoCase.ToString(),
                                                    String.IsNullOrWhiteSpace(failureSource) ? StackTraceMarkers.UnknownSourceMoniker : failureSource);
                    remoteTraceBuilder.AppendLine();
                }

                remoteTraceBuilder.AppendLine(failure.StackTrace.Trim());

                if (!alreadyMarkedUp)
                {
                    remoteTraceBuilder.AppendFormat(StackTraceMarkers.EndRemoteTraceTemplate,
                                                    exceptionType.Name,
                                                    failure.FailureInfoCase.ToString(),
                                                    String.IsNullOrWhiteSpace(failureSource) ? StackTraceMarkers.UnknownSourceMoniker : failureSource);
                    remoteTraceBuilder.AppendLine();
                }

                info.AddValue("RemoteStackTraceString", remoteTraceBuilder.ToString(), typeof(string));
            }

            Exception failureException = await exceptionFactory(failure, info, payloadConverter, payloadCodec, cancelToken);
            return failureException;
        }

        private static void GetNetExceptionTypeInfo(Failure failure,
                                                    out Type exceptionType,
                                                    out Func<Failure,
                                                             SerializationInfo,
                                                             IPayloadConverter,
                                                             IPayloadCodec,
                                                             CancellationToken,
                                                             Task<Exception>> exceptionFactory)
        {
            switch (failure.FailureInfoCase)
            {
                case Failure.FailureInfoOneofCase.TimeoutFailureInfo:
                    exceptionType = typeof(TimeoutException);
                    exceptionFactory = async (failure, info, payloadConverter, payloadCodec, cancelToken) =>
                    {
                        PayloadContainers.IUnnamed details = await DeserializeAsync(failure.TimeoutFailureInfo.LastHeartbeatDetails,
                                                                                    payloadConverter,
                                                                                    payloadCodec,
                                                                                    cancelToken);

                        info.AddValue(nameof(TimeoutException.TimeoutType), (int) failure.TimeoutFailureInfo.TimeoutType);
                        info.AddValue(nameof(TimeoutException.LastHeartbeatDetails), details, typeof(PayloadContainers.IUnnamed));

                        return new TimeoutException(info, new StreamingContext(StreamingContextStates.CrossMachine));
                    };

                    return;

                case Failure.FailureInfoOneofCase.CanceledFailureInfo:
                    exceptionType = typeof(CancellationException);
                    exceptionFactory = async (failure, info, payloadConverter, payloadCodec, cancelToken) =>
                    {
                        PayloadContainers.IUnnamed details = await DeserializeAsync(failure.CanceledFailureInfo.Details,
                                                                                    payloadConverter,
                                                                                    payloadCodec,
                                                                                    cancelToken);

                        info.AddValue(nameof(CancellationException.Details), details, typeof(PayloadContainers.IUnnamed));

                        return new CancellationException(info, new StreamingContext(StreamingContextStates.CrossMachine));
                    };

                    return;

                case Failure.FailureInfoOneofCase.TerminatedFailureInfo:
                    exceptionType = typeof(TerminationException);
                    exceptionFactory = (failure, info, payloadConverter, payloadCodec, cancelToken) =>
                    {
                        return Task.FromResult((Exception) new TerminationException(info, new StreamingContext(StreamingContextStates.CrossMachine)));
                    };

                    return;

                case Failure.FailureInfoOneofCase.ServerFailureInfo:
                    exceptionType = typeof(OrchesrationServerException);
                    exceptionFactory = (failure, info, payloadConverter, payloadCodec, cancelToken) =>
                    {
                        info.AddValue(nameof(OrchesrationServerException.IsNonRetryable), failure.ServerFailureInfo.NonRetryable);

                        return Task.FromResult((Exception) new OrchesrationServerException(info, new StreamingContext(StreamingContextStates.CrossMachine)));
                    };

                    return;

                case Failure.FailureInfoOneofCase.ResetWorkflowFailureInfo:
                    exceptionType = typeof(ResetWorkflowException);
                    exceptionFactory = async (failure, info, payloadConverter, payloadCodec, cancelToken) =>
                    {
                        PayloadContainers.IUnnamed details = await DeserializeAsync(failure.ResetWorkflowFailureInfo.LastHeartbeatDetails,
                                                                                    payloadConverter,
                                                                                    payloadCodec,
                                                                                    cancelToken);

                        info.AddValue(nameof(ResetWorkflowException.LastHeartbeatDetails), details, typeof(PayloadContainers.IUnnamed));

                        return new ResetWorkflowException(info, new StreamingContext(StreamingContextStates.CrossMachine));
                    };

                    return;

                case Failure.FailureInfoOneofCase.ActivityFailureInfo:
                    exceptionType = typeof(ActivityException);
                    exceptionFactory = (failure, info, payloadConverter, payloadCodec, cancelToken) =>
                    {
                        info.AddValue(nameof(ActivityException.ScheduledEventId), failure.ActivityFailureInfo.ScheduledEventId);
                        info.AddValue(nameof(ActivityException.StartedEventId), failure.ActivityFailureInfo.StartedEventId);
                        info.AddValue(nameof(ActivityException.Identity), failure.ActivityFailureInfo.Identity, typeof(string));
                        info.AddValue(nameof(ActivityException.ActivityTypeName), failure.ActivityFailureInfo.ActivityType.Name, typeof(string));
                        info.AddValue(nameof(ActivityException.ActivityId), failure.ActivityFailureInfo.ActivityId, typeof(string));
                        info.AddValue(nameof(ActivityException.RetryState), (int) failure.ActivityFailureInfo.RetryState);

                        return Task.FromResult((Exception) new ActivityException(info, new StreamingContext(StreamingContextStates.CrossMachine)));
                    };

                    return;

                case Failure.FailureInfoOneofCase.ChildWorkflowExecutionFailureInfo:
                    exceptionType = typeof(ChildWorkflowException);
                    exceptionFactory = (failure, info, payloadConverter, payloadCodec, cancelToken) =>
                    {
                        info.AddValue(nameof(ChildWorkflowException.Namespace), failure.ChildWorkflowExecutionFailureInfo.Namespace, typeof(string));
                        info.AddValue(nameof(ChildWorkflowException.WorkflowId), failure.ChildWorkflowExecutionFailureInfo.WorkflowExecution.WorkflowId, typeof(string));
                        info.AddValue(nameof(ChildWorkflowException.WorkflowRunId), failure.ChildWorkflowExecutionFailureInfo.WorkflowExecution.RunId, typeof(string));
                        info.AddValue(nameof(ChildWorkflowException.WorkflowTypeName), failure.ChildWorkflowExecutionFailureInfo.WorkflowType.Name, typeof(string));
                        info.AddValue(nameof(ChildWorkflowException.InitiatedEventId), failure.ChildWorkflowExecutionFailureInfo.InitiatedEventId);
                        info.AddValue(nameof(ChildWorkflowException.StartedEventId), failure.ChildWorkflowExecutionFailureInfo.StartedEventId);
                        info.AddValue(nameof(ChildWorkflowException.RetryState), (int) failure.ChildWorkflowExecutionFailureInfo.RetryState);

                        return Task.FromResult((Exception) new ChildWorkflowException(info, new StreamingContext(StreamingContextStates.CrossMachine)));
                    };

                    return;

                case Failure.FailureInfoOneofCase.ApplicationFailureInfo:
                    exceptionType = typeof(ApplicationException);
                    exceptionFactory = async (failure, info, payloadConverter, payloadCodec, cancelToken) =>
                    {
                        PayloadContainers.IUnnamed details = await DeserializeAsync(failure.ApplicationFailureInfo.Details,
                                                                                    payloadConverter,
                                                                                    payloadCodec,
                                                                                    cancelToken);

                        info.AddValue(nameof(ApplicationException.Type), failure.ApplicationFailureInfo.Type, typeof(string));
                        info.AddValue(nameof(ApplicationException.IsNonRetryable), failure.ApplicationFailureInfo.NonRetryable);
                        info.AddValue(nameof(ApplicationException.Details), details, typeof(PayloadContainers.IUnnamed));

                        return new ApplicationException(info, new StreamingContext(StreamingContextStates.CrossMachine));
                    };

                    return;

                case Failure.FailureInfoOneofCase.None:
                default:
                    exceptionType = typeof(ApplicationException);
                    exceptionFactory = (failure, info, payloadConverter, payloadCodec, cancelToken) =>
                    {
                        return Task.FromResult((Exception) new ApplicationException(info, new StreamingContext(StreamingContextStates.CrossMachine)));
                    };

                    return;
            }
        }

        private static async Task<PayloadContainers.IUnnamed> DeserializeAsync(Payloads payloads,
                                                                               IPayloadConverter payloadConverter,
                                                                               IPayloadCodec payloadCodec,
                                                                               CancellationToken cancelToken)
        {
            if (payloads == null)
            {
                return null;
            }

            if (payloadCodec != null)
            {
                payloads = await payloadCodec.DecodeAsync(payloads, cancelToken);
            }

            return payloadConverter.Deserialize<PayloadContainers.IUnnamed>(payloads);
        }

        public static ITemporalFailure GetInnerTemporalFailure(this WorkflowConcludedAbnormallyException rtEx)
        {
            Validate.NotNull(rtEx);
            return rtEx.InnerException.Cast<Exception, ITemporalFailure>();
        }
    }
}
