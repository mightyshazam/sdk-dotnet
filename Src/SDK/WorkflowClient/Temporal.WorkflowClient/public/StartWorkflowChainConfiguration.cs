using System;
using Temporal.Api.Common.V1;
using Temporal.Api.Enums.V1;

namespace Temporal.WorkflowClient
{
    public class StartWorkflowChainConfiguration
    {
        public static StartWorkflowChainConfiguration Default { get { throw new NotImplementedException(); } }

        public TimeSpan WorkflowExecutionTimeout { get; init; }
        public TimeSpan WorkflowRunTimeout { get; init; }
        public TimeSpan WorkflowTaskTimeout { get; init; }
        public string Identity { get; init; }
        public WorkflowIdReusePolicy WorkflowIdReusePolicy { get; init; }
        public RetryPolicy RetryPolicy { get; init; }
        public string CronSchedule { get; init; }
        public Memo Memo { get; init; }
        public SearchAttributes SearchAttributes { get; init; }
        public Header Header { get; init; }

        // Settings that are ot user-facing:
        // string RequestId 
    }
}
