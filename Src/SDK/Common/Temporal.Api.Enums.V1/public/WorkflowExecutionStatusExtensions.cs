using System;

namespace Temporal.Api.Enums.V1
{
    public static class WorkflowExecutionStatusExtensions
    {
        public static bool IsTerminal(this WorkflowExecutionStatus status)
        {
            switch (status)
            {
                case WorkflowExecutionStatus.Unspecified:
                case WorkflowExecutionStatus.Running:
                    return false;
                case WorkflowExecutionStatus.Completed:
                case WorkflowExecutionStatus.Failed:
                case WorkflowExecutionStatus.Canceled:
                case WorkflowExecutionStatus.Terminated:
                case WorkflowExecutionStatus.ContinuedAsNew:
                case WorkflowExecutionStatus.TimedOut:
                    return true;
                default:
                    throw new ArgumentException($"Invalid {nameof(WorkflowExecutionStatus)} value: {status.ToString()} (= {(int) status}).");
            }
        }
    }
}
