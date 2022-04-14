using System;
using System.Runtime.Serialization;
using Candidly.Util;
using Temporal.Common.Payloads;

namespace Temporal.WorkflowClient.Errors
{
    public sealed class CancellationException : Exception, ITemporalFailure
    {
        public IUnnamedValuesContainer Details { get; }

        public CancellationException(string message)
            : this(message, details: null, innerException: null)
        {
        }

        public CancellationException(string message, IUnnamedValuesContainer details, Exception innerException)
            : base(Format.TrimSafe(message), innerException)
        {
            Details = details ?? new PayloadContainers.ForUnnamedValues.Empty();
        }

        internal CancellationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Details = (IUnnamedValuesContainer) info.GetValue(nameof(Details), typeof(IUnnamedValuesContainer))
                                        ?? new PayloadContainers.ForUnnamedValues.Empty();
        }
    }
}
