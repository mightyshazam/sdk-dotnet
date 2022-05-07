using System;
using System.Text;
using Temporal.Util;
using Temporal.Api.Enums.V1;

namespace Temporal.WorkflowClient.Errors
{
    public sealed class WorkflowQueryException : Exception
    {
        private static string FormatMessage(string message,
                                            string queryTypeName,
                                            WorkflowExecutionStatus workflowStatus,
                                            string @namespace,
                                            string workflowId,
                                            string workflowChainId,
                                            string workflowRunId,
                                            Exception innerException)
        {
            StringBuilder msg = ExceptionMessage.GetBasis<WorkflowConcludedAbnormallyException>(message,
                                                                                                innerException,
                                                                                                out int basisMsgLength);

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(QueryTypeName), basisMsgLength))
            {
                msg.Append(nameof(QueryTypeName) + "=");
                msg.Append(Format.QuoteOrNull(queryTypeName));
            }

            if (ExceptionMessage.StartNextInfoItemIfRequired(msg, message, nameof(WorkflowStatus), basisMsgLength))
            {
                msg.Append(nameof(WorkflowStatus) + "='");
                msg.Append(workflowStatus.ToString());
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

        public WorkflowQueryException(string message,
                                      string queryTypeName,
                                      WorkflowExecutionStatus workflowStatus,
                                      string @namespace,
                                      string workflowId,
                                      string workflowChainId,
                                      string workflowRunId)
            : this(message, queryTypeName, workflowStatus, @namespace, workflowId, workflowChainId, workflowRunId, innerException: null)
        {
        }

        public WorkflowQueryException(string message,
                                      string queryTypeName,
                                      string @namespace,
                                      string workflowId,
                                      string workflowChainId,
                                      string workflowRunId,
                                      Exception innerException)
            : this(message, queryTypeName, WorkflowExecutionStatus.Unspecified, @namespace, workflowId, workflowChainId, workflowRunId, innerException)
        {
        }

        public WorkflowQueryException(string message,
                                      string queryTypeName,
                                      WorkflowExecutionStatus workflowStatus,
                                      string @namespace,
                                      string workflowId,
                                      string workflowChainId,
                                      string workflowRunId,
                                      Exception innerException)
            : base(FormatMessage(message, queryTypeName, workflowStatus, @namespace, workflowId, workflowChainId, workflowRunId, innerException),
                   innerException)
        {
            QueryTypeName = queryTypeName;
            WorkflowStatus = workflowStatus;
            Namespace = @namespace;
            WorkflowId = workflowId;
            WorkflowChainId = workflowChainId;
            WorkflowRunId = workflowRunId;
        }

        public string QueryTypeName { get; }
        public WorkflowExecutionStatus WorkflowStatus { get; }
        public string Namespace { get; }
        public string WorkflowId { get; }
        public string WorkflowChainId { get; }
        public string WorkflowRunId { get; }
    }
}
