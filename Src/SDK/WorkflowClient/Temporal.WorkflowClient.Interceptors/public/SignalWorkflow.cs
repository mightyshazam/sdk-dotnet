using System.Threading;

namespace Temporal.WorkflowClient.Interceptors
{
    public static class SignalWorkflow
    {
        public record Arguments<TSigArg>(string Namespace,
                                         string WorkflowId,
                                         string WorkflowChainId,
                                         string WorkflowRunId,
                                         string SignalName,
                                         TSigArg SignalArg,
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
