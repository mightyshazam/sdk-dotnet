using System;
using System.Runtime.Serialization;
using System.Text;
using Candidly.Util;
using Temporal.Api.Enums.V1;
using Temporal.Api.Failure.V1;

namespace Temporal.WorkflowClient.Errors
{
    public sealed class ActivityException : Exception, ITemporalFailure
    {
        #region Static APIs
        public static ActivityException CreateFromPayload(Failure failurePayload,
                                                          Exception innerException,
                                                          int innerExceptionChainDepth)
        {
            TemporalFailure.ValidateFailurePayloadKind(failurePayload, Failure.FailureInfoOneofCase.ActivityFailureInfo);

            SerializationInfo info = TemporalFailure.CreateSerializationInfoWithCommonData<ActivityException>
                                                                                          (failurePayload,
                                                                                           innerException,
                                                                                           innerExceptionChainDepth);
            info.AddValue("Message",
                          FormatMessage(failurePayload.Message,
                                        failurePayload.ActivityFailureInfo.ActivityType.Name,
                                        failurePayload.ActivityFailureInfo.ActivityId,
                                        failurePayload.ActivityFailureInfo.ScheduledEventId,
                                        failurePayload.ActivityFailureInfo.StartedEventId,
                                        failurePayload.ActivityFailureInfo.Identity,
                                        failurePayload.ActivityFailureInfo.RetryState,
                                        innerException),
                          typeof(string));

            info.AddValue(nameof(ActivityTypeName), failurePayload.ActivityFailureInfo.ActivityType.Name, typeof(string));
            info.AddValue(nameof(ActivityId), failurePayload.ActivityFailureInfo.ActivityId, typeof(string));
            info.AddValue(nameof(ScheduledEventId), failurePayload.ActivityFailureInfo.ScheduledEventId);
            info.AddValue(nameof(StartedEventId), failurePayload.ActivityFailureInfo.StartedEventId);
            info.AddValue(nameof(Identity), failurePayload.ActivityFailureInfo.Identity, typeof(string));
            info.AddValue(nameof(RetryState), (int) failurePayload.ActivityFailureInfo.RetryState);

            return new ActivityException(info, new StreamingContext(StreamingContextStates.CrossMachine));
        }

        private static string FormatMessage(string message,
                                            string activityTypeName,
                                            string activityId,
                                            long scheduledEventId,
                                            long startedEventId,
                                            string identity,
                                            RetryState retryState,
                                            Exception innerException)
        {
            StringBuilder msg = ExceptionMessage.GetBasis<ActivityException>(message, innerException, out int basisMsgLength);

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(ActivityTypeName), basisMsgLength))
            {
                msg.Append(nameof(ActivityTypeName) + "=");
                msg.Append(Format.QuoteOrNull(activityTypeName));
            }

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(ActivityId), basisMsgLength))
            {
                msg.Append(nameof(ActivityId) + "=");
                msg.Append(Format.QuoteOrNull(activityId));
            }

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(ScheduledEventId), basisMsgLength))
            {
                msg.Append(nameof(ScheduledEventId) + "=");
                msg.Append(scheduledEventId);
            }

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(StartedEventId), basisMsgLength))
            {
                msg.Append(nameof(StartedEventId) + "=");
                msg.Append(startedEventId);
            }

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(Identity), basisMsgLength))
            {
                msg.Append(nameof(Identity) + "=");
                msg.Append(Format.QuoteOrNull(identity));
            }

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(RetryState), basisMsgLength))
            {
                msg.Append(nameof(RetryState) + "='");
                msg.Append(retryState.ToString());
                msg.Append('\'');
            }

            ExceptionMessage.CompleteInfoItems(msg, basisMsgLength);
            return msg.ToString();
        }
        #endregion Static APIs

        public string ActivityTypeName { get; }
        public string ActivityId { get; }
        public long ScheduledEventId { get; }
        public long StartedEventId { get; }
        public string Identity { get; }
        public RetryState RetryState { get; }

        public ActivityException(string message,
                                 string activityTypeName,
                                 string activityId,
                                 long scheduledEventId,
                                 long startedEventId,
                                 string identity,
                                 RetryState retryState,
                                 Exception innerException)
            : base(FormatMessage(message, activityTypeName, activityId, scheduledEventId, startedEventId, identity, retryState, innerException),
                   innerException)
        {
            ActivityTypeName = activityTypeName ?? String.Empty;
            ActivityId = activityId ?? String.Empty;
            ScheduledEventId = scheduledEventId;
            StartedEventId = startedEventId;
            Identity = identity ?? String.Empty;
            RetryState = retryState;
        }

        internal ActivityException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ActivityTypeName = info.GetString(nameof(ActivityTypeName)) ?? String.Empty;
            ActivityId = info.GetString(nameof(ActivityId)) ?? String.Empty;
            ScheduledEventId = info.GetInt64(nameof(ScheduledEventId));
            StartedEventId = info.GetInt64(nameof(StartedEventId));
            Identity = info.GetString(nameof(Identity)) ?? String.Empty;
            RetryState = (RetryState) info.GetInt32(nameof(RetryState));
        }
    }
}
