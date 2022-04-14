using System;
using System.Runtime.Serialization;
using Candidly.Util;

namespace Temporal.WorkflowClient.Errors
{
    public sealed class OrchesrationServerException : Exception, ITemporalFailure
    {
        public bool IsNonRetryable { get; }

        public OrchesrationServerException(string message, bool isNonRetryable)
            : this(message, isNonRetryable, innerException: null)
        {
        }

        public OrchesrationServerException(string message, bool isNonRetryable, Exception innerException)
            : base(Format.TrimSafe(message), innerException)
        {
            IsNonRetryable = isNonRetryable;
        }

        internal OrchesrationServerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            IsNonRetryable = info.GetBoolean(nameof(IsNonRetryable));
        }
    }
}
