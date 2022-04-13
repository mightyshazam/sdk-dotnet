using System;
using Temporal.Api.Common.V1;
using Temporal.Api.Enums.V1;

namespace Temporal.WorkflowClient
{
    public class StartWorkflowChainConfiguration
    {
        private static readonly StartWorkflowChainConfiguration s_default = new StartWorkflowChainConfiguration()
        {
            WorkflowExecutionTimeout = null,
            WorkflowRunTimeout = null,
            WorkflowTaskTimeout = null,
            Identity = null,
            WorkflowIdReusePolicy = null,
            RetryPolicy = null,
            CronSchedule = null,
            Memo = null,
            SearchAttributes = null,
            Header = null
        };

        public static StartWorkflowChainConfiguration Default
        {
            get { return s_default; }
        }

        public TimeSpan? WorkflowExecutionTimeout { get; init; }
        public TimeSpan? WorkflowRunTimeout { get; init; }
        public TimeSpan? WorkflowTaskTimeout { get; init; }
        public string Identity { get; init; }
        public WorkflowIdReusePolicy? WorkflowIdReusePolicy { get; init; }
        public RetryPolicy RetryPolicy { get; init; }
        public string CronSchedule { get; init; }
        public Memo Memo { get; init; }
        public SearchAttributes SearchAttributes { get; init; }
        public Header Header { get; init; }

        // Settings that are ot user-facing:
        // string RequestId 
    }
}
