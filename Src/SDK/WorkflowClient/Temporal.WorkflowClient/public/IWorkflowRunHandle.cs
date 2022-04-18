using System;

namespace Temporal.WorkflowClient
{
    public interface IWorkflowRunHandle
    {
        string WorkflowRunId { get; }
    }
}
