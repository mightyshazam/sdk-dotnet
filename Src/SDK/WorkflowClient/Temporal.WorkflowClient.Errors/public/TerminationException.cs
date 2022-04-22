using System;
using System.Runtime.Serialization;
using System.Text;
using Temporal.Api.Failure.V1;

namespace Temporal.WorkflowClient.Errors
{
    public sealed class TerminationException : Exception, ITemporalFailure
    {
        #region Static APIs
        public static TerminationException CreateFromPayload(Failure failurePayload,
                                                             Exception innerException,
                                                             int innerExceptionChainDepth)
        {
            TemporalFailure.ValidateFailurePayloadKind(failurePayload, Failure.FailureInfoOneofCase.TerminatedFailureInfo);

            SerializationInfo info = TemporalFailure.CreateSerializationInfoWithCommonData<TerminationException>
                                                                                          (failurePayload,
                                                                                           innerException,
                                                                                           innerExceptionChainDepth);
            info.AddValue("Message",
                          FormatMessage(failurePayload.Message, innerException),
                          typeof(string));

            return new TerminationException(info, new StreamingContext(StreamingContextStates.CrossMachine));
        }

        private static string FormatMessage(string message,
                                           Exception innerException)
        {
            StringBuilder msg = ExceptionMessage.GetBasis<ResetWorkflowException>(message, innerException, out int _);
            return msg.ToString();
        }
        #endregion Static APIs

        public TerminationException(string message)
            : this(message, innerException: null)
        {
        }

        public TerminationException(string message, Exception innerException)
            : base(FormatMessage(message, innerException), innerException)
        {
        }

        internal TerminationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
