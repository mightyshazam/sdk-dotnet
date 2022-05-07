using System;

namespace Temporal.WorkflowClient.Interceptors
{
    public record ServiceInvocationPipelineItemFactoryArguments(ITemporalClient ServiceClient,
                                                                object InitialPipelineOwner,
                                                                IWorkflowOperationArguments InitialOperationArguments)
    {
    }
}
