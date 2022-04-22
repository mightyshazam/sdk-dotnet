using System.Threading;

namespace Temporal.WorkflowClient.Interceptors
{
    public static class TerminateWorkflow
    {
        public record Arguments<TTermArg>(string Namespace,
                                          string WorkflowId,
                                          string WorkflowChainId,
                                          string WorkflowRunId,
                                          string Reason,
                                          TTermArg Details,
                                          CancellationToken CancelToken) : IWorkflowOperationArguments;

        public class Result : IWorkflowOperationResult
        {
            public Result(string workflowChainId)
            {
                ValidateWorkflowProperty.ChainId.Bound(workflowChainId);
                WorkflowChainId = workflowChainId;
            }

            public string WorkflowChainId { get; }

            public bool TryGetBoundWorkflowChainId(out string workflowChainId)
            {
                workflowChainId = WorkflowChainId;
                return (workflowChainId != null);
            }
        }
    }
}
