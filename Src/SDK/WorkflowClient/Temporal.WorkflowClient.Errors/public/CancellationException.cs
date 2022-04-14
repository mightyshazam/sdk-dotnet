using System;
using System.Runtime.Serialization;
using Candidly.Util;
using Temporal.Common.Payloads;

namespace Temporal.WorkflowClient.Errors
{
    public sealed class CancellationException : Exception, ITemporalFailure
    {
        public PayloadContainers.IUnnamed Details { get; }

        public CancellationException(string message)
            : this(message, details: null, innerException: null)
        {
        }

        public CancellationException(string message, PayloadContainers.IUnnamed details, Exception innerException)
            : base(Format.TrimSafe(message), innerException)
        {
            Details = details ?? new PayloadContainers.Unnamed.Empty();
        }

        internal CancellationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Details = (PayloadContainers.IUnnamed) info.GetValue(nameof(Details), typeof(PayloadContainers.IUnnamed))
                                        ?? new PayloadContainers.Unnamed.Empty();
        }
    }
}
