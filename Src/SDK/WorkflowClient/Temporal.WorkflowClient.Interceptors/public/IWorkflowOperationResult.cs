namespace Temporal.WorkflowClient.Interceptors
{
    public interface IWorkflowOperationResult
    {
        bool TryGetBoundWorkflowChainId(out string workflowChainId);
    }
}
