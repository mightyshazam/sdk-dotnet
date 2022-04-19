
using System.Threading;

namespace Temporal.WorkflowClient.Interceptors
{
    public static class GetWorkflowChainId
    {
        public record Arguments(string Namespace,
                                string WorkflowId,
                                string WorkflowRunId,
                                CancellationToken CancelToken);

        public record Result(string WorkflowChainId);
    }
}
