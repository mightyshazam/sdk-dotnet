using System.Threading;

namespace Temporal.WorkflowClient.Interceptors
{
    public static class AwaitConclusion
    {
        public record Arguments(string Namespace,
                                string WorkflowId,
                                string WorkflowChainId,
                                string WorkflowRunId,
                                bool FollowWorkflowChain,
                                CancellationToken CancelToken);
    }
}
