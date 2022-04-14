using System;
using Temporal.Api.Enums.V1;
using Temporal.Common.Payloads;
using Temporal.WorkflowClient.Interceptors;

namespace Temporal.WorkflowClient
{
    public interface IWorkflowRunResult : IWorkflowChainBindingResult
    {
        string Namespace { get; }
        string WorkflowId { get; }
        string WorkflowRunId { get; }

        bool IsConcludedSuccessfully { get; }

        WorkflowExecutionStatus Status { get; }

        Exception Failure { get; }

        bool IsContinuedAsNew { get; }
        bool TryGetContinuationRun(out IWorkflowRun continuationRunHandle);

        object ConclusionEventAttributes { get; }

        /// <summary>
        /// Throws for any Status except OK. This method backs GetResult(..) on WorkflowChain.
        /// </summary>
        TVal GetValue<TVal>();
        PayloadContainers.IUnnamed GetValue();

        /// <summary>
        /// Doesn't throw on non-OK Status. Can be used to retrieve payloads that came with non-OK (aka non-Completed) terminal events.
        /// </summary>
        TVal GetPayload<TVal>();
        PayloadContainers.IUnnamed GetPayload();

        bool TryGetPayload<TVal>(out TVal deserializedPayload);
        bool TryGetPayload(out PayloadContainers.IUnnamed deserializedPayload);
    }
}
