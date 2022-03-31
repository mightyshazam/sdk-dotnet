namespace Temporal.WorkflowClient.Interceptors
{
    public interface IWorkflowChainBindingResult
    {
        bool TryGetBoundWorkflowChainId(out string workflowChainId);
    }
}
