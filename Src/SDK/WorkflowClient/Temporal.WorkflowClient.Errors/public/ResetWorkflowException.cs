using System;
using System.Runtime.Serialization;
using Candidly.Util;
using Temporal.Common.Payloads;

namespace Temporal.WorkflowClient.Errors
{
    public sealed class ResetWorkflowException : Exception, ITemporalFailure
    {
        public IUnnamedValuesContainer LastHeartbeatDetails { get; }

        public ResetWorkflowException(string message)
            : this(message, lastHeartbeatDetails: null, innerException: null)
        {
        }

        public ResetWorkflowException(string message, IUnnamedValuesContainer lastHeartbeatDetails, Exception innerException)
            : base(Format.TrimSafe(message), innerException)
        {
            LastHeartbeatDetails = lastHeartbeatDetails ?? new PayloadContainers.ForUnnamedValues.Empty();
        }

        internal ResetWorkflowException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            LastHeartbeatDetails = (IUnnamedValuesContainer) info.GetValue(nameof(LastHeartbeatDetails), typeof(IUnnamedValuesContainer))
                                        ?? new PayloadContainers.ForUnnamedValues.Empty();
        }
    }
}
