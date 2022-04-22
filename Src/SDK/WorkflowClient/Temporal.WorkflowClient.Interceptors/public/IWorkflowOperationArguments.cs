using System;
using System.Threading;

namespace Temporal.WorkflowClient.Interceptors
{
    public interface IWorkflowOperationArguments
    {
        string Namespace { get; }
        string WorkflowId { get; }
        string WorkflowChainId { get; }
        string WorkflowRunId { get; }
        CancellationToken CancelToken { get; }
    }
}
