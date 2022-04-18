using System;
using System.Text;
using Temporal.Util;
using Temporal.Api.Enums.V1;

namespace Temporal.WorkflowClient.Errors
{
    public sealed class WorkflowConcludedAbnormallyException : Exception
    {
        private static Exception AsException(ITemporalFailure failure)
        {
            if (failure == null)
            {
                return null;
            }

            if (failure is not Exception exception)
            {
                throw new ArgumentException($"The type of the specified instance of {nameof(ITemporalFailure)} must"
                                          + $" be a subclass of {nameof(Exception)}, but it is not the case for the"
                                          + $" actual runtime type (\"{failure.GetType().FullName}\").",
                                            nameof(failure));
            }

            return exception;
        }

        private static ITemporalFailure AsTemporalFailure(Exception exception)
        {
            if (exception == null)
            {
                return null;
            }

            if (exception is not ITemporalFailure failure)
            {
                throw new ArgumentException($"The type of the specified instance of {nameof(Exception)} must"
                                          + $" implement the interface {nameof(ITemporalFailure)}, but it is not the case for the"
                                          + $" actual runtime type (\"{exception.GetType().FullName}\").",
                                            nameof(exception));
            }

            return failure;
        }

        private static string FormatMessage(string message,
                                            WorkflowExecutionStatus conclusionStatus,
                                            string @namespace,
                                            string workflowId,
                                            string workflowChainId,
                                            string workflowRunId,
                                            ITemporalFailure innerException)
        {
            StringBuilder msg = ExceptionMessage.GetBasis<WorkflowConcludedAbnormallyException>(message,
                                                                                                AsException(innerException),
                                                                                                out int basisMsgLength);

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(Namespace), basisMsgLength))
            {
                msg.Append(nameof(ConclusionStatus) + "='");
                msg.Append(conclusionStatus.ToString());
                msg.Append('\'');
            }

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

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(WorkflowChainId), basisMsgLength))
            {
                msg.Append(nameof(WorkflowChainId) + "=");
                msg.Append(Format.QuoteOrNull(workflowChainId));
            }

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(WorkflowRunId), basisMsgLength))
            {
                msg.Append(nameof(WorkflowRunId) + "=");
                msg.Append(Format.QuoteOrNull(workflowRunId));
            }

            ExceptionMessage.CompleteInfoItems(msg, basisMsgLength);
            return msg.ToString();
        }

        public WorkflowConcludedAbnormallyException(string message,
                                                    WorkflowExecutionStatus conclusionStatus,
                                                    string @namespace,
                                                    string workflowId,
                                                    string workflowChainId,
                                                    string workflowRunId,
                                                    Exception innerException)
            : this(message, conclusionStatus, @namespace, workflowId, workflowChainId, workflowRunId, AsTemporalFailure(innerException))
        {
        }

        public WorkflowConcludedAbnormallyException(string message,
                                                    WorkflowExecutionStatus conclusionStatus,
                                                    string @namespace,
                                                    string workflowId,
                                                    string workflowChainId,
                                                    string workflowRunId,
                                                    ITemporalFailure innerException)
            : base(FormatMessage(message, conclusionStatus, @namespace, workflowId, workflowChainId, workflowRunId, innerException),
                   AsException(innerException))
        {
            ConclusionStatus = conclusionStatus;
            Namespace = @namespace;
            WorkflowId = workflowId;
            WorkflowChainId = workflowChainId;
            WorkflowRunId = workflowRunId;
        }

        public WorkflowExecutionStatus ConclusionStatus { get; }
        public string Namespace { get; }
        public string WorkflowId { get; }
        public string WorkflowChainId { get; }
        public string WorkflowRunId { get; }
    }
}
