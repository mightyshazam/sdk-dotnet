using System.Threading;
using Temporal.WorkflowClient.OperationConfigurations;

namespace Temporal.WorkflowClient.Interceptors
{
    public static class QueryWorkflow
    {
        public record Arguments<TQryArg>(string Namespace,
                                         string WorkflowId,
                                         string WorkflowChainId,
                                         string WorkflowRunId,
                                         string QueryName,
                                         TQryArg QueryArg,
                                         QueryWorkflowConfiguration QueryConfig,
                                         CancellationToken CancelToken) : IWorkflowOperationArguments;

        public class Result<TResult> : IWorkflowOperationResult
        {
            private readonly TResult _resultValue;

            public Result(TResult resultValue, string workflowChainId)
            {
                ValidateWorkflowProperty.ChainId.Bound(workflowChainId);
                WorkflowChainId = workflowChainId;
                _resultValue = resultValue;
            }

            public string WorkflowChainId { get; }

            public TResult Value { get { return _resultValue; } }

            public bool TryGetBoundWorkflowChainId(out string workflowChainId)
            {
                workflowChainId = WorkflowChainId;
                return (workflowChainId != null);
            }
        }
    }
}
