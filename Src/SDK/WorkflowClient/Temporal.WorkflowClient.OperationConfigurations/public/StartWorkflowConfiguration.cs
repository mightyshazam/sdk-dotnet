using System;
using Temporal.Api.Common.V1;
using Temporal.Api.Enums.V1;

namespace Temporal.WorkflowClient.OperationConfigurations
{
    public record StartWorkflowConfiguration(TimeSpan? WorkflowExecutionTimeout,
                                             TimeSpan? WorkflowRunTimeout,
                                             TimeSpan? WorkflowTaskTimeout,
                                             WorkflowIdReusePolicy? WorkflowIdReusePolicy,
                                             RetryPolicy RetryPolicy,
                                             string CronSchedule,
                                             Memo Memo,
                                             SearchAttributes SearchAttributes,
                                             Header Header)
    {
        private static readonly StartWorkflowConfiguration s_default = new();

        public static StartWorkflowConfiguration Default
        {
            get { return s_default; }
        }

        public StartWorkflowConfiguration()
            : this(WorkflowExecutionTimeout: null,
                   WorkflowRunTimeout: null,
                   WorkflowTaskTimeout: null,
                   WorkflowIdReusePolicy: null,
                   RetryPolicy: null,
                   CronSchedule: null,
                   Memo: null,
                   SearchAttributes: null,
                   Header: null)
        {
        }

        // Settings that are not user-facing:
        // string RequestId 
    }
}
