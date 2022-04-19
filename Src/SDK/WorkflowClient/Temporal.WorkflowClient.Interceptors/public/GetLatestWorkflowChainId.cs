
using System.Threading;

namespace Temporal.WorkflowClient.Interceptors
{
    public static class GetLatestWorkflowChainId
    {
        public record Arguments(string Namespace,
                                string WorkflowId,
                                CancellationToken CancelToken);

        public record Result(string WorkflowChainId);
    }
}
