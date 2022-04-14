using System;
using System.Runtime.Serialization;
using Candidly.Util;
using Temporal.Api.Enums.V1;
using Temporal.Common.Payloads;

namespace Temporal.WorkflowClient.Errors
{
    public sealed class TimeoutException : Exception, ITemporalFailure
    {
        public TimeoutType TimeoutType { get; }
        public PayloadContainers.IUnnamed LastHeartbeatDetails { get; }

        public TimeoutException(string message, TimeoutType timeoutType)
            : this(message, timeoutType, lastHeartbeatDetails: null, innerException: null)
        {
        }

        public TimeoutException(string message, TimeoutType timeoutType, PayloadContainers.IUnnamed lastHeartbeatDetails, Exception innerException)
            : base(Format.TrimSafe(message), innerException)
        {
            TimeoutType = timeoutType;
            LastHeartbeatDetails = lastHeartbeatDetails ?? new PayloadContainers.Unnamed.Empty();
        }

        internal TimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            TimeoutType = (TimeoutType) info.GetInt32(nameof(TimeoutType));
            LastHeartbeatDetails = (PayloadContainers.IUnnamed) info.GetValue(nameof(LastHeartbeatDetails), typeof(PayloadContainers.IUnnamed))
                                        ?? new PayloadContainers.Unnamed.Empty();
        }
    }
}
