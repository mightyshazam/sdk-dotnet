using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Temporal.Common.WorkflowConfiguration
{
    public class CommonWorkflowConfigurationApi
    {
    }

    /// <summary>
    /// Per-workflow settings related to the execution container of a workflow.
    /// Must not affect the business logic.
    /// These may optionally be set by both: by the client that stated a workflow OR by the workflow host (globally or for a specific workflow).
    /// If set in several places, these settings will be merged before being applied to a specific workflow at the time of starting it.
    /// Once started, they need to be read-only.
    /// Example: Timeouts.
    /// </summary>
    public interface IWorkflowExecutionConfiguration
    {
        int WorkflowTaskTimeoutMillisec { get; }
        string TaskQueue { get; }

        // Add:
        // workflowIdReusePolicy, workflowRunTimeout, workflowExecutionTimeout
        // taskQueue, retryOptions, cronSchedule, memo, searchAttributes

        // Do NOT add:
        // workflow / run id, namespace, workflow type
    }

    /// <summary>
    /// Per-workflow settings related to the execution container of a workflow.
    /// Must not affect the business logic.
    /// These may optionally be set by both: by the client that stated a workflow OR by the workflow host (globally or for a specific workflow).
    /// If set in several places, these settings will be merged before being applied to a specific workflow at the time of starting it.
    /// Once started, they need to be read-only.
    /// Example: Timeouts.
    /// </summary>   
    public class WorkflowExecutionConfiguration : IWorkflowExecutionConfiguration
    {
        public int WorkflowTaskTimeoutMillisec { get; set; }
        public string TaskQueue { get; set; }
        public WorkflowExecutionConfiguration() { }
        public WorkflowExecutionConfiguration(string taskQueue) { TaskQueue = taskQueue; }
    }
}
