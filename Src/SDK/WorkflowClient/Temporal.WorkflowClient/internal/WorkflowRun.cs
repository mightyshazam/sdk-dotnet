using System;

namespace Temporal.WorkflowClient
{
    internal class WorkflowRun
    {
        public static void ValidateWorkflowRunId(string workflowRunId)
        {
            if (workflowRunId != null && String.IsNullOrWhiteSpace(workflowRunId))
            {
                throw new ArgumentException($"{nameof(workflowRunId)} must be either null, or a non-empty-or-whitespace string."
                                          + $" However, \"{workflowRunId}\" was specified.");
            }
        }

        public string WorkflowRunId { get; }
    }
}
