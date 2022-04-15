using System;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Temporal.Api.Failure.V1;
using Temporal.Common.Payloads;
using Temporal.Serialization;

namespace Temporal.WorkflowClient.Errors
{
    public sealed class CancellationException : Exception, ITemporalFailure
    {
        #region Static APIs
        public static async Task<CancellationException> CreateFromPayloadAsync(Failure failurePayload,
                                                                               Exception innerException,
                                                                               int innerExceptionChainDepth,
                                                                               IPayloadConverter payloadConverter,
                                                                               IPayloadCodec payloadCodec,
                                                                               CancellationToken cancelToken)
        {
            TemporalFailure.ValidateFailurePayloadKind(failurePayload, Failure.FailureInfoOneofCase.CanceledFailureInfo);

            SerializationInfo info = TemporalFailure.CreateSerializationInfoWithCommonData<CancellationException>
                                                                                          (failurePayload,
                                                                                           innerException,
                                                                                           innerExceptionChainDepth);

            PayloadContainers.IUnnamed details = await TemporalFailure.DeserializeDetailsAsync(
                                                                                           failurePayload.CanceledFailureInfo.Details,
                                                                                           payloadConverter,
                                                                                           payloadCodec,
                                                                                           cancelToken);
            info.AddValue("Message",
                          FormatMessage(failurePayload.Message, details, innerException),
                          typeof(string));

            info.AddValue(nameof(Details), details, typeof(PayloadContainers.IUnnamed));

            return new CancellationException(info, new StreamingContext(StreamingContextStates.CrossMachine));
        }

        private static string FormatMessage(string message,
                                            PayloadContainers.IUnnamed details,
                                            Exception innerException)
        {
            StringBuilder msg = ExceptionMessage.GetBasis<CancellationException>(message, innerException, out int basisMsgLength);

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(Details), basisMsgLength))
            {
                msg.Append(nameof(Details) + ": ");
                msg.Append(details == null ? "none" : (details.Count + " entries"));
            }

            ExceptionMessage.CompleteInfoItems(msg, basisMsgLength);
            return msg.ToString();
        }
        #endregion Static APIs

        public PayloadContainers.IUnnamed Details { get; }

        public CancellationException(string message)
            : this(message, details: null, innerException: null)
        {
        }

        public CancellationException(string message, PayloadContainers.IUnnamed details, Exception innerException)
            : base(FormatMessage(message, details, innerException), innerException)
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
