using System;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Candidly.Util;
using Temporal.Api.Failure.V1;
using Temporal.Common.Payloads;
using Temporal.Serialization;

namespace Temporal.WorkflowClient.Errors
{
    public sealed class ApplicationException : Exception, ITemporalFailure
    {
        #region Static APIs
        public static async Task<ApplicationException> CreateFromPayloadAsync(Failure failurePayload,
                                                                              Exception innerException,
                                                                              int innerExceptionChainDepth,
                                                                              IPayloadConverter payloadConverter,
                                                                              IPayloadCodec payloadCodec,
                                                                              CancellationToken cancelToken)
        {
            Validate.NotNull(failurePayload);

            SerializationInfo info = TemporalFailure.CreateSerializationInfoWithCommonData<ApplicationException>
                                                                                          (failurePayload,
                                                                                           innerException,
                                                                                           innerExceptionChainDepth);

            if (failurePayload.FailureInfoCase == Failure.FailureInfoOneofCase.ApplicationFailureInfo)
            {
                PayloadContainers.IUnnamed details = await TemporalFailure.DeserializeDetailsAsync(
                                                                                           failurePayload.ApplicationFailureInfo.Details,
                                                                                           payloadConverter,
                                                                                           payloadCodec,
                                                                                           cancelToken);

                info.AddValue("Message",
                              FormatMessage(failurePayload.Message,
                                            failurePayload.ApplicationFailureInfo.Type,
                                            failurePayload.ApplicationFailureInfo.NonRetryable,
                                            details,
                                            innerException),
                          typeof(string));

                info.AddValue(nameof(Type), failurePayload.ApplicationFailureInfo.Type, typeof(string));
                info.AddValue(nameof(IsNonRetryable), failurePayload.ApplicationFailureInfo.NonRetryable);
                info.AddValue(nameof(Details), details, typeof(PayloadContainers.IUnnamed));
            }
            else
            {
                info.AddValue("Message",
                              FormatMessage(failurePayload.Message, type: null, isNonRetryable: false, details: null, innerException),
                              typeof(string));
            }

            return new ApplicationException(info, new StreamingContext(StreamingContextStates.CrossMachine));
        }

        private static string FormatMessage(string message,
                                            string type,
                                            bool isNonRetryable,
                                            PayloadContainers.IUnnamed details,
                                            Exception innerException)
        {
            StringBuilder msg = ExceptionMessage.GetBasis<ApplicationException>(message, innerException, out int basisMsgLength);

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(Type), basisMsgLength))
            {
                msg.Append(nameof(Type) + "=");
                msg.Append(Format.QuoteOrNull(type));
            }

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(IsNonRetryable), basisMsgLength))
            {
                msg.Append(nameof(IsNonRetryable) + "=");
                msg.Append(isNonRetryable.ToString());
            }

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(Details), basisMsgLength))
            {
                msg.Append(nameof(Details) + ": ");
                msg.Append(details == null ? "none" : (details.Count + " entries"));
            }

            ExceptionMessage.CompleteInfoItems(msg, basisMsgLength);
            return msg.ToString();
        }
        #endregion Static APIs

        public string Type { get; }
        public bool IsNonRetryable { get; }
        public PayloadContainers.IUnnamed Details { get; }

        public ApplicationException(string message, bool isNonRetryable)
            : this(message, type: null, isNonRetryable, details: null, innerException: null)
        {
        }

        public ApplicationException(string message, string type, bool isNonRetryable, PayloadContainers.IUnnamed details, Exception innerException)
            : base(FormatMessage(message, type, isNonRetryable, details, innerException), innerException)
        {
            Type = Format.TrimSafe(type);
            IsNonRetryable = isNonRetryable;
            Details = details ?? new PayloadContainers.Unnamed.Empty();
        }

        internal ApplicationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // Failure payloads are deselialized into ApplicationException if FailureInfoOneofCase is
            // not pointing to any particular exception type.
            // Thus, the fields below are optional and we need to be resilient towards them not being present.

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
