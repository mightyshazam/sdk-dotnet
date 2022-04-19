using System;
using Temporal.Util;

namespace Temporal.WorkflowClient
{
    internal class WorkflowRunHandle : IWorkflowRunHandle
    {
        #region static APIs

        public static void ValidateWorkflowRunId(string workflowRunId)
        {
            if (workflowRunId != null && String.IsNullOrWhiteSpace(workflowRunId))
            {
                throw new ArgumentException($"{nameof(workflowRunId)} must be either null, or a non-empty-or-whitespace string."
                                          + $" However, \"{workflowRunId}\" was specified.");
            }
        }

        #endregion static APIs

        private IWorkflowHandle _ownerWorkflow = null;

        public WorkflowRunHandle(string workflowId, string workflowRunId)
        {
            Validate.NotNull(workflowId);
            Validate.NotNull(workflowRunId);

            WorkflowId = workflowId;
            WorkflowRunId = workflowRunId;
        }

        public WorkflowRunHandle(IWorkflowHandle workflow, string workflowRunId)
        {
            Validate.NotNull(workflow);
            Validate.NotNull(workflowRunId);

            WorkflowId = workflow.WorkflowId;
            WorkflowRunId = workflowRunId;
            _ownerWorkflow = workflow;
        }

        public string WorkflowId { get; }
        public string WorkflowRunId { get; }
    }
}
