using System;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Temporal.Api.Enums.V1;
using Temporal.Api.Failure.V1;
using Temporal.Common.Payloads;
using Temporal.Serialization;

namespace Temporal.WorkflowClient.Errors
{
    public sealed class TimeoutException : Exception, ITemporalFailure
    {
        #region Static APIs
        public static async Task<TimeoutException> CreateFromPayloadAsync(Failure failurePayload,
                                                                          Exception innerException,
                                                                          int innerExceptionChainDepth,
                                                                          IPayloadConverter payloadConverter,
                                                                          IPayloadCodec payloadCodec,
                                                                          CancellationToken cancelToken)
        {
            TemporalFailure.ValidateFailurePayloadKind(failurePayload, Failure.FailureInfoOneofCase.TimeoutFailureInfo);

            SerializationInfo info = TemporalFailure.CreateSerializationInfoWithCommonData<TimeoutException>
                                                                                          (failurePayload,
                                                                                           innerException,
                                                                                           innerExceptionChainDepth);

            PayloadContainers.IUnnamed details = await TemporalFailure.DeserializeDetailsAsync(
                                                                                           failurePayload.TimeoutFailureInfo.LastHeartbeatDetails,
                                                                                           payloadConverter,
                                                                                           payloadCodec,
                                                                                           cancelToken);

            info.AddValue("Message",
                          FormatMessage(failurePayload.Message, failurePayload.TimeoutFailureInfo.TimeoutType, details, innerException),
                          typeof(string));

            info.AddValue(nameof(TimeoutType), (int) failurePayload.TimeoutFailureInfo.TimeoutType);
            info.AddValue(nameof(LastHeartbeatDetails), details, typeof(PayloadContainers.IUnnamed));

            return new TimeoutException(info, new StreamingContext(StreamingContextStates.CrossMachine));
        }

        private static string FormatMessage(string message,
                                            TimeoutType timeoutType,
                                            PayloadContainers.IUnnamed lastHeartbeatDetails,
                                            Exception innerException)
        {
            StringBuilder msg = ExceptionMessage.GetBasis<TimeoutException>(message, innerException, out int basisMsgLength);

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(TimeoutType), basisMsgLength))
            {
                msg.Append(nameof(TimeoutType) + "='");
                msg.Append(timeoutType.ToString());
                msg.Append('\'');
            }

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(LastHeartbeatDetails), basisMsgLength))
            {
                msg.Append(nameof(LastHeartbeatDetails) + ": ");
                msg.Append(lastHeartbeatDetails == null ? "none" : (lastHeartbeatDetails.Count + " entries"));
            }

            ExceptionMessage.CompleteInfoItems(msg, basisMsgLength);
            return msg.ToString();
        }
        #endregion Static APIs

        public TimeoutType TimeoutType { get; }
        public PayloadContainers.IUnnamed LastHeartbeatDetails { get; }

        public TimeoutException(string message, TimeoutType timeoutType)
            : this(message, timeoutType, lastHeartbeatDetails: null, innerException: null)
        {
        }

        public TimeoutException(string message, TimeoutType timeoutType, PayloadContainers.IUnnamed lastHeartbeatDetails, Exception innerException)
            : base(FormatMessage(message, timeoutType, lastHeartbeatDetails, innerException), innerException)
        {
            TimeoutType = timeoutType;
            LastHeartbeatDetails = lastHeartbeatDetails ?? new PayloadContainers.Unnamed.Empty();
        }

        internal TimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            TimeoutType = (TimeoutType) info.GetInt32(nameof(TimeoutType));
            LastHeartbeatDetails = (PayloadContainers.IUnnamed) info.GetValue(nameof(LastHeartbeatDetails), typeof(PayloadContainers.IUnnamed))
                                        ?? new PayloadContainers.Unnamed.Empty();
        }
    }
}
