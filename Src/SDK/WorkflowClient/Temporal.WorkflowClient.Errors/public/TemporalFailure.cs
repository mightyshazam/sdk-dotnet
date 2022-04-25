using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Temporal.Util;
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

        public static ITemporalFailure GetInnerTemporalFailure(this WorkflowConcludedAbnormallyException rtEx)
        {
            Validate.NotNull(rtEx);
            return rtEx.InnerException.Cast<Exception, ITemporalFailure>();
        }

        public static Exception AsException(this ITemporalFailure failure)
        {
            if (failure == null)
            {
                return null;
            }

            if (failure is not Exception exception)
            {
                throw new ArgumentException($"The type of the specified instance of {nameof(ITemporalFailure)} must"
                                          + $" be a subclass of {nameof(Exception)}, but it is not the case for the"
                                          + $" actual runtime type (\"{failure.GetType().FullName}\").",
                                            nameof(failure));
            }

            return exception;
        }

        internal static ITemporalFailure AsTemporalFailure(this Exception exception)
        {
            if (exception == null)
            {
                return null;
            }

            if (exception is not ITemporalFailure failure)
            {
                throw new ArgumentException($"The type of the specified instance of {nameof(Exception)} must"
                                          + $" implement the interface {nameof(ITemporalFailure)}, but it is not the case for the"
                                          + $" actual runtime type (\"{exception.GetType().FullName}\").",
                                            nameof(exception));
            }

            return failure;
        }

        public static async Task<Exception> FromPayloadAsync(Failure failurePayload,
                                                             IPayloadConverter payloadConverter,
                                                             IPayloadCodec payloadCodec,
                                                             CancellationToken cancelToken)
        {
            Validate.NotNull(failurePayload);

            if (failurePayload.Cause == null)
            {
                return await FromPayloadWithCauseAsync(failurePayload, cause: null, causeChainDepth: 0, payloadConverter, payloadCodec, cancelToken);
            }

            // Don't joke with how deeply nested this can be.
            // Unwind the cause chain so that we can rehydrate them in a non-recursive way.

            List<Failure> failures = new();
            Failure nextFailure = failurePayload;
            while (nextFailure != null)
            {
                failures.Add(nextFailure);
                nextFailure = nextFailure.Cause;
            }

            // Now build the chain starting from the most inner cause:

            Exception failureException = null;
            for (int f = failures.Count - 1; f >= 0; f--)
            {
                failureException = await FromPayloadWithCauseAsync(failures[f], failureException, f, payloadConverter, payloadCodec, cancelToken);
            }

            return failureException;
        }

        private static async Task<Exception> FromPayloadWithCauseAsync(Failure failure,
                                                                       Exception cause,
                                                                       int causeChainDepth,
                                                                       IPayloadConverter payloadConverter,
                                                                       IPayloadCodec payloadCodec,
                                                                       CancellationToken cancelToken)
        {
            switch (failure.FailureInfoCase)
            {
                case Failure.FailureInfoOneofCase.TimeoutFailureInfo:
                    return await TimeoutException.CreateFromPayloadAsync(failure, cause, causeChainDepth, payloadConverter, payloadCodec, cancelToken);

                case Failure.FailureInfoOneofCase.CanceledFailureInfo:
                    return await CancellationException.CreateFromPayloadAsync(failure, cause, causeChainDepth, payloadConverter, payloadCodec, cancelToken);

                case Failure.FailureInfoOneofCase.TerminatedFailureInfo:
                    return TerminationException.CreateFromPayload(failure, cause, causeChainDepth);

                case Failure.FailureInfoOneofCase.ServerFailureInfo:
                    return OrchesrationServerException.CreateFromPayload(failure, cause, causeChainDepth);

                case Failure.FailureInfoOneofCase.ResetWorkflowFailureInfo:
                    return await ResetWorkflowException.CreateFromPayloadAsync(failure, cause, causeChainDepth, payloadConverter, payloadCodec, cancelToken);

                case Failure.FailureInfoOneofCase.ActivityFailureInfo:
                    return ActivityException.CreateFromPayload(failure, cause, causeChainDepth);

                case Failure.FailureInfoOneofCase.ChildWorkflowExecutionFailureInfo:
                    return ChildWorkflowException.CreateFromPayload(failure, cause, causeChainDepth);

                case Failure.FailureInfoOneofCase.ApplicationFailureInfo:
                    return await ApplicationException.CreateFromPayloadAsync(failure, cause, causeChainDepth, payloadConverter, payloadCodec, cancelToken);

                case Failure.FailureInfoOneofCase.None:
                default:
                    return await ApplicationException.CreateFromPayloadAsync(failure, cause, causeChainDepth, payloadConverter, payloadCodec, cancelToken);
            }
        }

        internal static SerializationInfo CreateSerializationInfoWithCommonData<TEx>(Failure failure,
                                                                                     Exception innerException,
                                                                                     int innerExceptionChainDepth)
                                                                                where TEx : Exception
        {
            Type targetExceptionType = typeof(TEx);

            SerializationInfo info = new(targetExceptionType, new FormatterConverter());

            // This is a joint set of values set by Net Fx 4.6.x and Net 6.
            // Values not needed in all versions (e.g. Net 6 does not use class name) should be ignired during rehydration.

            // Populate the basics:

            string failureSource = failure.Source?.Trim();

            info.AddValue("ClassName", targetExceptionType.ToString(), typeof(string));
            info.AddValue("Data", null, typeof(System.Collections.IDictionary));
            info.AddValue("InnerException", innerException, typeof(Exception));
            info.AddValue("HelpURL", null, typeof(string));
            info.AddValue("StackTraceString", null, typeof(string));
            info.AddValue("RemoteStackIndex", innerExceptionChainDepth, typeof(int));
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
                                                    targetExceptionType.Name,
                                                    failure.FailureInfoCase.ToString(),
                                                    String.IsNullOrWhiteSpace(failureSource) ? StackTraceMarkers.UnknownSourceMoniker : failureSource);
                    remoteTraceBuilder.AppendLine();
                }

                remoteTraceBuilder.AppendLine(failure.StackTrace.Trim());

                if (!alreadyMarkedUp)
                {
                    remoteTraceBuilder.AppendFormat(StackTraceMarkers.EndRemoteTraceTemplate,
                                                    targetExceptionType.Name,
                                                    failure.FailureInfoCase.ToString(),
                                                    String.IsNullOrWhiteSpace(failureSource) ? StackTraceMarkers.UnknownSourceMoniker : failureSource);
                    remoteTraceBuilder.AppendLine();
                }

                info.AddValue("RemoteStackTraceString", remoteTraceBuilder.ToString(), typeof(string));
            }

            return info;
        }

        internal static async Task<PayloadContainers.IUnnamed> DeserializeDetailsAsync(Payloads payloads,
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

        internal static void ValidateFailurePayloadKind(Failure failurePayload, Failure.FailureInfoOneofCase expectedPayloadKind)
        {
            Validate.NotNull(failurePayload);

            if (failurePayload.FailureInfoCase != expectedPayloadKind)
            {
                throw new ArgumentException($"The {nameof(Failure.FailureInfoCase)} of the specified {failurePayload} is expected to be"
                                          + $" '{expectedPayloadKind.ToString()}', however, the actual value is"
                                          + $" '{failurePayload.FailureInfoCase.ToString()}' (={((int) failurePayload.FailureInfoCase)}).");
            }

        }
    }
}
