namespace Temporal.WorkflowClient.Interceptors
{
    public struct StartWorkflowResult : IWorkflowChainBindingResult
    {
        public enum Status
        {
            Unspecified = 0,
            OK = 128,
            AlreadyExists = 6,
        }

        public StartWorkflowResult(string workflowRunId, StartWorkflowResult.Status code)
        {
            WorkflowRunId = workflowRunId;
            Code = code;
        }

        public string WorkflowRunId { get; init; }
        public Status Code { get; init; }

        public bool TryGetBoundWorkflowChainId(out string workflowChainId)
        {
            if (Code == Status.OK)
            {
                workflowChainId = WorkflowRunId;
                return true;
            }

            workflowChainId = null;
            return false;
        }
    }
}
