using System;
using Candidly.Util;

namespace Temporal.WorkflowClient.Errors
{
    public class WorkflowAlreadyExistsException : InvalidOperationException
    {
        private static string FormatMessage(string @namespace, string workflowId)
        {
            return "The requested operation cannot be performed because a workflow with the specified attributes already exists."
                 + " Inner Exception may have additional details."
                 + $" (namespace={@namespace.QuoteOrNull()}; workflowId={workflowId.QuoteOrNull()})";
        }

        internal WorkflowAlreadyExistsException(string @namespace, string workflowId)
            : base(FormatMessage(@namespace, workflowId))
        {
            Namespace = @namespace;
            WorkflowId = workflowId;
        }

        internal WorkflowAlreadyExistsException(string @namespace, string workflowId, Exception innerException)
            : base(FormatMessage(@namespace, workflowId), innerException)
        {
            Namespace = @namespace;
            WorkflowId = workflowId;
        }

        public string Namespace { get; }
        public string WorkflowId { get; }
    }
}
