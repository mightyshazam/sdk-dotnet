namespace Temporal.WorkflowClient.Interceptors
{
    public static class TerminateWorkflow
    {
        public record Arguments<TTermArg>();

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
