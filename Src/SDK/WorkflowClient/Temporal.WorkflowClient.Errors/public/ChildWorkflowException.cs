using System;
using System.Runtime.Serialization;
using System.Text;
using Candidly.Util;
using Temporal.Api.Enums.V1;
using Temporal.Api.Failure.V1;

namespace Temporal.WorkflowClient.Errors
{
    public sealed class ChildWorkflowException : Exception, ITemporalFailure
    {
        #region Static APIs
        public static ChildWorkflowException CreateFromPayload(Failure failurePayload,
                                                               Exception innerException,
                                                               int innerExceptionChainDepth)
        {
            TemporalFailure.ValidateFailurePayloadKind(failurePayload, Failure.FailureInfoOneofCase.ChildWorkflowExecutionFailureInfo);

            SerializationInfo info = TemporalFailure.CreateSerializationInfoWithCommonData<ChildWorkflowException>
                                                                                          (failurePayload,
                                                                                           innerException,
                                                                                           innerExceptionChainDepth);
            info.AddValue("Message",
                          FormatMessage(failurePayload.Message,
                                        failurePayload.ChildWorkflowExecutionFailureInfo.Namespace,
                                        failurePayload.ChildWorkflowExecutionFailureInfo.WorkflowExecution.WorkflowId,
                                        failurePayload.ChildWorkflowExecutionFailureInfo.WorkflowExecution.RunId,
                                        failurePayload.ChildWorkflowExecutionFailureInfo.WorkflowType.Name,
                                        failurePayload.ChildWorkflowExecutionFailureInfo.InitiatedEventId,
                                        failurePayload.ChildWorkflowExecutionFailureInfo.StartedEventId,
                                        failurePayload.ChildWorkflowExecutionFailureInfo.RetryState,
                                        innerException),
                          typeof(string));

            info.AddValue(nameof(Namespace), failurePayload.ChildWorkflowExecutionFailureInfo.Namespace, typeof(string));
            info.AddValue(nameof(WorkflowId), failurePayload.ChildWorkflowExecutionFailureInfo.WorkflowExecution.WorkflowId, typeof(string));
            info.AddValue(nameof(WorkflowRunId), failurePayload.ChildWorkflowExecutionFailureInfo.WorkflowExecution.RunId, typeof(string));
            info.AddValue(nameof(WorkflowTypeName), failurePayload.ChildWorkflowExecutionFailureInfo.WorkflowType.Name, typeof(string));
            info.AddValue(nameof(InitiatedEventId), failurePayload.ChildWorkflowExecutionFailureInfo.InitiatedEventId);
            info.AddValue(nameof(StartedEventId), failurePayload.ChildWorkflowExecutionFailureInfo.StartedEventId);
            info.AddValue(nameof(RetryState), (int) failurePayload.ChildWorkflowExecutionFailureInfo.RetryState);

            return new ChildWorkflowException(info, new StreamingContext(StreamingContextStates.CrossMachine));
        }

        private static string FormatMessage(string message,
                                            string @namespace,
                                            string workflowId,
                                            string workflowRunId,
                                            string workflowTypeName,
                                            long initiatedEventId,
                                            long startedEventId,
                                            RetryState retryState,
                                            Exception innerException)
        {
            StringBuilder msg = ExceptionMessage.GetBasis<ChildWorkflowException>(message, innerException, out int basisMsgLength);

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(Namespace), basisMsgLength))
            {
                msg.Append(nameof(Namespace) + "=");
                msg.Append(Format.QuoteOrNull(@namespace));
            }

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(WorkflowId), basisMsgLength))
            {
                msg.Append(nameof(WorkflowId) + "=");
                msg.Append(Format.QuoteOrNull(workflowId));
            }

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(WorkflowRunId), basisMsgLength))
            {
                msg.Append(nameof(WorkflowRunId) + "=");
                msg.Append(Format.QuoteOrNull(workflowRunId));
            }

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(WorkflowTypeName), basisMsgLength))
            {
                msg.Append(nameof(WorkflowTypeName) + "=");
                msg.Append(Format.QuoteOrNull(workflowTypeName));
            }

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(InitiatedEventId), basisMsgLength))
            {
                msg.Append(nameof(InitiatedEventId) + "=");
                msg.Append(initiatedEventId);
            }

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(StartedEventId), basisMsgLength))
            {
                msg.Append(nameof(StartedEventId) + "=");
                msg.Append(startedEventId);
            }

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(RetryState), basisMsgLength))
            {
                msg.Append(nameof(RetryState) + "='");
                msg.Append(retryState.ToString());
                msg.Append('\'');
            }

            ExceptionMessage.CompleteInfoItems(msg, basisMsgLength);
            return msg.ToString();
        }
        #endregion Static APIs


        public string Namespace { get; }
        public string WorkflowId { get; }
        public string WorkflowRunId { get; }
        public string WorkflowTypeName { get; }
        public long InitiatedEventId { get; }
        public long StartedEventId { get; }
        public RetryState RetryState { get; }

        public ChildWorkflowException(string message,
                                      string @namespace,
                                      string workflowId,
                                      string workflowRunId,
                                      string workflowTypeName,
                                      long initiatedEventId,
                                      long startedEventId,
                                      RetryState retryState,
                                      Exception innerException)
            : base(FormatMessage(message,
                                 @namespace,
                                 workflowId,
                                 workflowRunId,
                                 workflowTypeName,
                                 initiatedEventId,
                                 startedEventId,
                                 retryState,
                                 innerException),
                   innerException)
        {
            Namespace = Format.TrimSafe(@namespace);
            WorkflowId = Format.TrimSafe(workflowId);
            WorkflowRunId = Format.TrimSafe(workflowRunId);
            WorkflowTypeName = Format.TrimSafe(workflowTypeName);
            InitiatedEventId = initiatedEventId;
            StartedEventId = startedEventId;
            RetryState = retryState;
        }

        internal ChildWorkflowException(SerializationInfo info, StreamingContext context)
                : base(info, context)
        {
            Namespace = Format.TrimSafe(info.GetString(nameof(Namespace)));
            WorkflowId = Format.TrimSafe(info.GetString(nameof(WorkflowId)));
            WorkflowRunId = Format.TrimSafe(info.GetString(nameof(WorkflowRunId)));
            WorkflowTypeName = Format.TrimSafe(info.GetString(nameof(WorkflowTypeName)));
            InitiatedEventId = info.GetInt64(nameof(InitiatedEventId));
            StartedEventId = info.GetInt64(nameof(StartedEventId));
            RetryState = (RetryState) info.GetInt32(nameof(RetryState));
        }
    }
}
