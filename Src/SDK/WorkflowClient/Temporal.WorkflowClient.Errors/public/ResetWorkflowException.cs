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
    public sealed class ResetWorkflowException : Exception, ITemporalFailure
    {
        #region Static APIs
        public static async Task<ResetWorkflowException> CreateFromPayloadAsync(Failure failurePayload,
                                                                                Exception innerException,
                                                                                int innerExceptionChainDepth,
                                                                                IPayloadConverter payloadConverter,
                                                                                IPayloadCodec payloadCodec,
                                                                                CancellationToken cancelToken)
        {
            TemporalFailure.ValidateFailurePayloadKind(failurePayload, Failure.FailureInfoOneofCase.ResetWorkflowFailureInfo);

            SerializationInfo info = TemporalFailure.CreateSerializationInfoWithCommonData<ResetWorkflowException>
                                                                                          (failurePayload,
                                                                                           innerException,
                                                                                           innerExceptionChainDepth);

            PayloadContainers.IUnnamed details = await TemporalFailure.DeserializeDetailsAsync(
                                                                                           failurePayload.ResetWorkflowFailureInfo.LastHeartbeatDetails,
                                                                                           payloadConverter,
                                                                                           payloadCodec,
                                                                                           cancelToken);

            info.AddValue("Message",
                          FormatMessage(failurePayload.Message, details, innerException),
                          typeof(string));

            info.AddValue(nameof(LastHeartbeatDetails), details, typeof(PayloadContainers.IUnnamed));

            return new ResetWorkflowException(info, new StreamingContext(StreamingContextStates.CrossMachine));
        }

        private static string FormatMessage(string message,
                                            PayloadContainers.IUnnamed lastHeartbeatDetails,
                                            Exception innerException)
        {
            StringBuilder msg = ExceptionMessage.GetBasis<ResetWorkflowException>(message, innerException, out int basisMsgLength);

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(LastHeartbeatDetails), basisMsgLength))
            {
                msg.Append(nameof(LastHeartbeatDetails) + ": ");
                msg.Append(lastHeartbeatDetails == null ? "none" : (lastHeartbeatDetails.Count + " entries"));
            }

            ExceptionMessage.CompleteInfoItems(msg, basisMsgLength);
            return msg.ToString();
        }
        #endregion Static APIs

        public PayloadContainers.IUnnamed LastHeartbeatDetails { get; }

        public ResetWorkflowException(string message)
            : this(message, lastHeartbeatDetails: null, innerException: null)
        {
        }

        public ResetWorkflowException(string message, PayloadContainers.IUnnamed lastHeartbeatDetails, Exception innerException)
            : base(FormatMessage(message, lastHeartbeatDetails, innerException), innerException)
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
