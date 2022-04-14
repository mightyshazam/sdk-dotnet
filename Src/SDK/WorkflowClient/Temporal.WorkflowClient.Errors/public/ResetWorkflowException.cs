using System;
using System.Runtime.Serialization;
using Candidly.Util;
using Temporal.Common.Payloads;

namespace Temporal.WorkflowClient.Errors
{
    public sealed class ResetWorkflowException : Exception, ITemporalFailure
    {
        public PayloadContainers.IUnnamed LastHeartbeatDetails { get; }

        public ResetWorkflowException(string message)
            : this(message, lastHeartbeatDetails: null, innerException: null)
        {
        }

        public ResetWorkflowException(string message, PayloadContainers.IUnnamed lastHeartbeatDetails, Exception innerException)
            : base(Format.TrimSafe(message), innerException)
        {
            LastHeartbeatDetails = lastHeartbeatDetails ?? new PayloadContainers.Unnamed.Empty();
        }

        internal ResetWorkflowException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            LastHeartbeatDetails = (PayloadContainers.IUnnamed) info.GetValue(nameof(LastHeartbeatDetails), typeof(PayloadContainers.IUnnamed))
                                        ?? new PayloadContainers.Unnamed.Empty();
        }
    }
}
