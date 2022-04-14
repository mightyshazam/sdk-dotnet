using System;
using System.Runtime.Serialization;
using Candidly.Util;
using Temporal.Api.Enums.V1;

namespace Temporal.WorkflowClient.Errors
{
    public sealed class ActivityException : Exception, ITemporalFailure
    {
        public long ScheduledEventId { get; }
        public long StartedEventId { get; }
        public string Identity { get; }
        public string ActivityTypeName { get; }
        public string ActivityId { get; }
        public RetryState RetryState { get; }

        public ActivityException(string message,
                                 long scheduledEventId,
                                 long startedEventId,
                                 string identity,
                                 string activityTypeName,
                                 string activityId,
                                 RetryState retryState,
                                 Exception innerException)
            : base(Format.TrimSafe(message), innerException)
        {
            ScheduledEventId = scheduledEventId;
            StartedEventId = startedEventId;
            Identity = Format.TrimSafe(identity);
            ActivityTypeName = Format.TrimSafe(activityTypeName);
            ActivityId = Format.TrimSafe(activityId);
            RetryState = retryState;
        }

        internal ActivityException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ScheduledEventId = info.GetInt64(nameof(ScheduledEventId));
            StartedEventId = info.GetInt64(nameof(StartedEventId));
            Identity = Format.TrimSafe(info.GetString(nameof(Identity)));
            ActivityTypeName = Format.TrimSafe(info.GetString(nameof(ActivityTypeName)));
            ActivityId = Format.TrimSafe(info.GetString(nameof(ActivityId)));
            RetryState = (RetryState) info.GetInt32(nameof(RetryState));
        }
    }
}
