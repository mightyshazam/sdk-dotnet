using System;
using System.Text;
using Temporal.Util;

namespace Temporal.WorkflowClient.Errors
{
    public class TemporalServiceException : Exception
    {
        private static string FormatMessage(string @namespace, string workflowId, string workflowRunId)
        {
            StringBuilder msg = new("An error occurred while communicating to the Temporal service."
                                  + " Inner Exception may have additional details."
                                  + " (namespace=");
            msg.Append(@namespace.QuoteOrNull());

            if (workflowId != null)
            {
                msg.Append("; workflowId=\"");
                msg.Append(workflowId);
                msg.Append('\"');
            }

            if (workflowRunId != null)
            {
                msg.Append("; workflowRunId=\"");
                msg.Append(workflowRunId);
                msg.Append('\"');
            }

            msg.Append(')');

            return msg.ToString();
        }

        internal TemporalServiceException(string @namespace)
            : base(FormatMessage(@namespace, workflowId: null, workflowRunId: null))
        {
            Namespace = @namespace;
            WorkflowId = null;
            WorkflowRunId = null;
        }

        internal TemporalServiceException(string @namespace, Exception innerException)
            : this(@namespace, workflowId: null, workflowRunId: null, innerException)
        {
        }

        internal TemporalServiceException(string @namespace, string workflowId, string workflowRunId, Exception innerException)
            : base(FormatMessage(@namespace, workflowId, workflowRunId), innerException)
        {
            Namespace = @namespace;
            WorkflowId = workflowId;
            WorkflowRunId = workflowRunId;
        }

        public string Namespace { get; }
        public string WorkflowId { get; }
        public string WorkflowRunId { get; }
    }
}
