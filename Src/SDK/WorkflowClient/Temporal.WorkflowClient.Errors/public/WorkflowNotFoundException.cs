using System;
using Temporal.Util;

namespace Temporal.WorkflowClient.Errors
{
    public class WorkflowNotFoundException : InvalidOperationException
    {
        private static string FormatMessage(string @namespace, string workflowId, string workflowRunId)
        {
            return "The requested operation cannot be performed because a workflow with the specified attributes cannot be found."
                 + " Inner Exception may have additional details."
                 + $" (namespace={@namespace.QuoteOrNull()}; workflowId={workflowId.QuoteOrNull()}; workflowRunId={workflowRunId.QuoteOrNull()})";
        }

        internal WorkflowNotFoundException(string @namespace, string workflowId, string workflowRunId)
            : base(FormatMessage(@namespace, workflowId, workflowRunId))
        {
            Namespace = @namespace;
            WorkflowId = workflowId;
            WorkflowRunId = workflowRunId;
        }

        internal WorkflowNotFoundException(string @namespace, string workflowId, string workflowRunId, Exception innerException)
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
