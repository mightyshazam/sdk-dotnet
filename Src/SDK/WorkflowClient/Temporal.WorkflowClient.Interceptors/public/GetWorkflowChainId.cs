
using System.Threading;

namespace Temporal.WorkflowClient.Interceptors
{
    public static class GetWorkflowChainId
    {
        public record Arguments(string Namespace,
                                string WorkflowId,
                                string WorkflowRunId,
                                CancellationToken CancelToken);

        public class Result : IWorkflowChainBindingResult
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
