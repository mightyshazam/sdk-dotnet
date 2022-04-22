using System;
using System.Threading;

namespace Temporal.WorkflowClient.Interceptors
{
    public static class GetWorkflowChainId
    {

        public record Arguments(string Namespace,
                                string WorkflowId,
                                string WorkflowRunId,
                                CancellationToken CancelToken) : IWorkflowOperationArguments
        {
            public string WorkflowChainId
            {
                get { throw new NotSupportedException($"{nameof(WorkflowChainId)} is not supported by {GetType().Name}."); }
            }
        }

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
