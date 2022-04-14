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
        public IUnnamedValuesContainer LastHeartbeatDetails { get; }

        public TimeoutException(string message, TimeoutType timeoutType)
            : this(message, timeoutType, lastHeartbeatDetails: null, innerException: null)
        {
        }

        public TimeoutException(string message, TimeoutType timeoutType, IUnnamedValuesContainer lastHeartbeatDetails, Exception innerException)
            : base(Format.TrimSafe(message), innerException)
        {
            TimeoutType = timeoutType;
            LastHeartbeatDetails = lastHeartbeatDetails ?? new PayloadContainers.ForUnnamedValues.Empty();
        }

        internal TimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            TimeoutType = (TimeoutType) info.GetInt32(nameof(TimeoutType));
            LastHeartbeatDetails = (IUnnamedValuesContainer) info.GetValue(nameof(LastHeartbeatDetails), typeof(IUnnamedValuesContainer))
                                        ?? new PayloadContainers.ForUnnamedValues.Empty();
        }
    }
}
