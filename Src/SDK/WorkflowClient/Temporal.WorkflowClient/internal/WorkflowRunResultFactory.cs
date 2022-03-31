using System;
using Candidly.Util;
using Temporal.Api.Enums.V1;
using Temporal.Api.History.V1;
using Temporal.Serialization;
using Temporal.WorkflowClient.Errors;

namespace Temporal.WorkflowClient
{
    internal struct WorkflowRunResultFactory
    {
        private readonly IDataConverter _dataConverter;
        private readonly string _namespace;
        private readonly string _workflowId;
        private readonly string _workflowChainId;  // Once the server supports it, this will be updated form each event attrs.

        public WorkflowRunResultFactory(IDataConverter dataConverter, string @namespace, string workflowId, string workflowChainId)
        {
            Validate.NotNull(dataConverter);
            Validate.NotNullOrWhitespace(@namespace);
            Validate.NotNullOrWhitespace(workflowId);
            WorkflowChain.ValidateWorkflowChainId(workflowChainId);

            _dataConverter = dataConverter;
            _namespace = @namespace;
            _workflowId = workflowId;
            _workflowChainId = workflowChainId;  // Once the server supports it, this will be updated form each event attrs.
        }

        public WorkflowRunResult ForCompleted(string workflowRunId,
                                              WorkflowExecutionCompletedEventAttributes eventAttributes)
        {
            Validate.NotNull(eventAttributes);
            Validate.NotNull(eventAttributes.Result);

            string continuationRunId = String.IsNullOrWhiteSpace(eventAttributes.NewExecutionRunId)
                                                ? null
                                                : eventAttributes.NewExecutionRunId;

            return new WorkflowRunResult(_dataConverter,
                                         _namespace,
                                         _workflowId,
                                         _workflowChainId,
                                         workflowRunId,
                                         WorkflowExecutionStatus.Completed,
                                         serializedPayloads: eventAttributes.Result,
                                         failure: null,
                                         continuationRunId,
                                         eventAttributes);
        }

        public WorkflowRunResult ForFailed(string workflowRunId,
                                           WorkflowExecutionFailedEventAttributes eventAttributes)
        {
            Validate.NotNull(eventAttributes);
            string continuationRunId = String.IsNullOrWhiteSpace(eventAttributes.NewExecutionRunId)
                                                ? null
                                                : eventAttributes.NewExecutionRunId;

            return new WorkflowRunResult(_dataConverter,
                                         _namespace,
                                         _workflowId,
                                         _workflowChainId,
                                         workflowRunId,
                                         WorkflowExecutionStatus.Failed,
                                         serializedPayloads: null,
                                         failure: TemporalFailure.FromMessage(eventAttributes.Failure),
                                         continuationRunId,
                                         eventAttributes);
        }

        public WorkflowRunResult ForTimedOut(string workflowRunId,
                                             WorkflowExecutionTimedOutEventAttributes eventAttributes)
        {
            Validate.NotNull(eventAttributes);
            string continuationRunId = String.IsNullOrWhiteSpace(eventAttributes.NewExecutionRunId)
                                                ? null
                                                : eventAttributes.NewExecutionRunId;

            return new WorkflowRunResult(_dataConverter,
                                         _namespace,
                                         _workflowId,
                                         _workflowChainId,
                                         workflowRunId,
                                         WorkflowExecutionStatus.TimedOut,
                                         serializedPayloads: null,
                                         failure: null,
                                         continuationRunId,
                                         eventAttributes);
        }

        public WorkflowRunResult ForCanceled(string workflowRunId,
                                             WorkflowExecutionCanceledEventAttributes eventAttributes)
        {
            Validate.NotNull(eventAttributes);
            Validate.NotNull(eventAttributes.Details);
            return new WorkflowRunResult(_dataConverter,
                                         _namespace,
                                         _workflowId,
                                         _workflowChainId,
                                         workflowRunId,
                                         WorkflowExecutionStatus.Canceled,
                                         serializedPayloads: eventAttributes.Details,
                                         failure: null,
                                         continuationRunId: null,
                                         eventAttributes);
        }

        public WorkflowRunResult ForTerminated(string workflowRunId,
                                               WorkflowExecutionTerminatedEventAttributes eventAttributes)
        {
            Validate.NotNull(eventAttributes);
            return new WorkflowRunResult(_dataConverter,
                                         _namespace,
                                         _workflowId,
                                         _workflowChainId,
                                         workflowRunId,
                                         WorkflowExecutionStatus.Terminated,
                                         serializedPayloads: eventAttributes.Details, // these may be null!
                                         failure: null,
                                         continuationRunId: null,
                                         eventAttributes);
        }

        public WorkflowRunResult ForContinuedAsNew(string workflowRunId,
                                                   WorkflowExecutionContinuedAsNewEventAttributes eventAttributes)
        {
            Validate.NotNull(eventAttributes);
            Validate.NotNull(eventAttributes.LastCompletionResult);
            return new WorkflowRunResult(_dataConverter,
                                         _namespace,
                                         _workflowId,
                                         _workflowChainId,
                                         workflowRunId,
                                         WorkflowExecutionStatus.ContinuedAsNew,
                                         serializedPayloads: eventAttributes.LastCompletionResult,
                                         failure: TemporalFailure.FromMessage(eventAttributes.Failure),
                                         continuationRunId: eventAttributes.NewExecutionRunId,
                                         eventAttributes);
        }
    }
}
