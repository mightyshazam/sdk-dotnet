using System;
using System.Runtime.Serialization;
using Candidly.Util;

namespace Temporal.WorkflowClient.Errors
{
    public sealed class TerminationException : Exception, ITemporalFailure
    {
        public TerminationException(string message)
            : this(message, innerException: null)
        {
        }

        public TerminationException(string message, Exception innerException)
            : base(Format.TrimSafe(message), innerException)
        {
        }

        internal TerminationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
