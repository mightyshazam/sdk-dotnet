using System;
using System.Runtime.Serialization;
using Candidly.Util;
using Temporal.Common.Payloads;

namespace Temporal.WorkflowClient.Errors
{
    public sealed class ApplicationException : Exception, ITemporalFailure
    {
        public string Type { get; }
        public bool IsNonRetryable { get; }
        public PayloadContainers.IUnnamed Details { get; }

        public ApplicationException(string message, bool isNonRetryable)
            : this(message, type: null, isNonRetryable, details: null, innerException: null)
        {
        }

        public ApplicationException(string message, string type, bool isNonRetryable, PayloadContainers.IUnnamed details, Exception innerException)
            : base(Format.TrimSafe(message), innerException)
        {
            Type = Format.TrimSafe(type);
            IsNonRetryable = isNonRetryable;
            Details = details ?? new PayloadContainers.Unnamed.Empty();
        }

        internal ApplicationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // Failure payloads are deselialized into ApplicationException if FailureInfoOneofCase is
            // not pointing to any particular exception type. Thus, the fields below are optional and
            // we need to be resilient towards them not being present.

            try
            {
                Type = Format.TrimSafe(info.GetString(nameof(Type)));
            }
            catch (SerializationException)
            {
                Type = String.Empty;
            }

            try
            {
                IsNonRetryable = info.GetBoolean(nameof(IsNonRetryable));
            }
            catch (SerializationException)
            {
                IsNonRetryable = false;
            }

            try
            {
                Details = (PayloadContainers.IUnnamed) info.GetValue(nameof(Details), typeof(PayloadContainers.IUnnamed))
                                        ?? new PayloadContainers.Unnamed.Empty();
            }
            catch (SerializationException)
            {
                Details = new PayloadContainers.Unnamed.Empty();
            }
        }
    }
}
