using System;
using System.Runtime.Serialization;
using System.Text;
using Temporal.Util;
using Temporal.Api.Failure.V1;

namespace Temporal.WorkflowClient.Errors
{
    public sealed class OrchesrationServerException : Exception, ITemporalFailure
    {
        #region Static APIs
        public static OrchesrationServerException CreateFromPayload(Failure failurePayload,
                                                                    Exception innerException,
                                                                    int innerExceptionChainDepth)
        {
            TemporalFailure.ValidateFailurePayloadKind(failurePayload, Failure.FailureInfoOneofCase.ServerFailureInfo);

            SerializationInfo info = TemporalFailure.CreateSerializationInfoWithCommonData<OrchesrationServerException>
                                                                                          (failurePayload,
                                                                                           innerException,
                                                                                           innerExceptionChainDepth);

            info.AddValue("Message",
                          FormatMessage(failurePayload.Message, failurePayload.ServerFailureInfo.NonRetryable, innerException),
                          typeof(string));

            info.AddValue(nameof(IsNonRetryable), failurePayload.ServerFailureInfo.NonRetryable);

            return new OrchesrationServerException(info, new StreamingContext(StreamingContextStates.CrossMachine));
        }

        private static string FormatMessage(string message,
                                           bool isNonRetryable,
                                           Exception innerException)
        {
            StringBuilder msg = ExceptionMessage.GetBasis<OrchesrationServerException>(message, innerException, out int basisMsgLength);

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(IsNonRetryable), basisMsgLength))
            {
                msg.Append(nameof(IsNonRetryable) + "=");
                msg.Append(isNonRetryable.ToString());
            }

            ExceptionMessage.CompleteInfoItems(msg, basisMsgLength);
            return msg.ToString();
        }
        #endregion Static APIs

        public bool IsNonRetryable { get; }

        public OrchesrationServerException(string message, bool isNonRetryable)
            : this(message, isNonRetryable, innerException: null)
        {
        }

        public OrchesrationServerException(string message, bool isNonRetryable, Exception innerException)
            : base(FormatMessage(message, isNonRetryable, innerException), innerException)
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
