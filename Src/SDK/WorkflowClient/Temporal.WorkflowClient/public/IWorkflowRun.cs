using System;

namespace Temporal.WorkflowClient
{
    public interface IWorkflowRun
    {
        string WorkflowRunId { get; }
    }
}
