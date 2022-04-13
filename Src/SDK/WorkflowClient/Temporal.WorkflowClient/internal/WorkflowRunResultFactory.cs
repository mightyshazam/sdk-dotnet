using System;
using System.Threading;
using System.Threading.Tasks;
using Candidly.Util;
using Temporal.Api.Common.V1;
using Temporal.Api.Enums.V1;
using Temporal.Api.History.V1;
using Temporal.Serialization;
using Temporal.WorkflowClient.Errors;

namespace Temporal.WorkflowClient
{
    internal struct WorkflowRunResultFactory
    {
        private readonly IPayloadConverter _payloadConverter;
        private readonly IPayloadCodec _payloadCodec;
        private readonly string _namespace;
        private readonly string _workflowId;
        private readonly string _workflowChainId;  // Once the server supports it, this will be updated form each event attrs.

        public WorkflowRunResultFactory(IPayloadConverter payloadConverter,
                                        IPayloadCodec payloadCodec,
                                        string @namespace,
                                        string workflowId,
                                        string workflowChainId)
        {
            Validate.NotNull(payloadConverter);
            Validate.NotNullOrWhitespace(@namespace);
            Validate.NotNullOrWhitespace(workflowId);
            WorkflowChain.ValidateWorkflowChainId(workflowChainId);

            _payloadConverter = payloadConverter;
            _payloadCodec = payloadCodec;  // may be null
            _namespace = @namespace;
            _workflowId = workflowId;
            _workflowChainId = workflowChainId;  // Once the server supports it, this will be updated form each event attrs.
        }

        public async Task<WorkflowRunResult> ForCompletedAsync(string workflowRunId,
                                                               WorkflowExecutionCompletedEventAttributes eventAttributes,
                                                               CancellationToken cancelToken)
        {
            Validate.NotNull(eventAttributes);
            Validate.NotNull(eventAttributes.Result);

            string continuationRunId = String.IsNullOrWhiteSpace(eventAttributes.NewExecutionRunId)
                                                ? null
                                                : eventAttributes.NewExecutionRunId;

            return new WorkflowRunResult(_payloadConverter,
                                         _namespace,
                                         _workflowId,
                                         _workflowChainId,
                                         workflowRunId,
                                         WorkflowExecutionStatus.Completed,
                                         await DecodePayloads(eventAttributes.Result, cancelToken),
                                         failure: null,
                                         continuationRunId,
                                         eventAttributes);
        }

        public Task<WorkflowRunResult> ForFailedAsync(string workflowRunId,
                                                      WorkflowExecutionFailedEventAttributes eventAttributes,
                                                      CancellationToken _)
        {
            Validate.NotNull(eventAttributes);
            string continuationRunId = String.IsNullOrWhiteSpace(eventAttributes.NewExecutionRunId)
                                                ? null
                                                : eventAttributes.NewExecutionRunId;

            return Task.FromResult(new WorkflowRunResult(_payloadConverter,
                                                         _namespace,
                                                         _workflowId,
                                                         _workflowChainId,
                                                         workflowRunId,
                                                         WorkflowExecutionStatus.Failed,
                                                         serializedPayloads: null,
                                                         failure: TemporalFailure.FromMessage(eventAttributes.Failure),
                                                         continuationRunId,
                                                         eventAttributes));
        }

        public Task<WorkflowRunResult> ForTimedOutAsync(string workflowRunId,
                                                        WorkflowExecutionTimedOutEventAttributes eventAttributes,
                                                        CancellationToken _)
        {
            Validate.NotNull(eventAttributes);
            string continuationRunId = String.IsNullOrWhiteSpace(eventAttributes.NewExecutionRunId)
                                                ? null
                                                : eventAttributes.NewExecutionRunId;

            return Task.FromResult(new WorkflowRunResult(_payloadConverter,
                                                         _namespace,
                                                         _workflowId,
                                                         _workflowChainId,
                                                         workflowRunId,
                                                         WorkflowExecutionStatus.TimedOut,
                                                         serializedPayloads: null,
                                                         failure: null,
                                                         continuationRunId,
                                                         eventAttributes));
        }

        public async Task<WorkflowRunResult> ForCanceledAsync(string workflowRunId,
                                                              WorkflowExecutionCanceledEventAttributes eventAttributes,
                                                              CancellationToken cancelToken)
        {
            Validate.NotNull(eventAttributes);
            Validate.NotNull(eventAttributes.Details);
            return new WorkflowRunResult(_payloadConverter,
                                         _namespace,
                                         _workflowId,
                                         _workflowChainId,
                                         workflowRunId,
                                         WorkflowExecutionStatus.Canceled,
                                         await DecodePayloads(eventAttributes.Details, cancelToken),
                                         failure: null,
                                         continuationRunId: null,
                                         eventAttributes);
        }

        public async Task<WorkflowRunResult> ForTerminatedAsync(string workflowRunId,
                                                                WorkflowExecutionTerminatedEventAttributes eventAttributes,
                                                                CancellationToken cancelToken)
        {
            Validate.NotNull(eventAttributes);
            return new WorkflowRunResult(_payloadConverter,
                                         _namespace,
                                         _workflowId,
                                         _workflowChainId,
                                         workflowRunId,
                                         WorkflowExecutionStatus.Terminated,
                                         await DecodePayloads(eventAttributes.Details, cancelToken),  // Details may be null!
                                         failure: null,
                                         continuationRunId: null,
                                         eventAttributes);
        }

        public async Task<WorkflowRunResult> ForContinuedAsNewAsync(string workflowRunId,
                                                                    WorkflowExecutionContinuedAsNewEventAttributes eventAttributes,
                                                                    CancellationToken cancelToken)
        {
            Validate.NotNull(eventAttributes);
            Validate.NotNull(eventAttributes.LastCompletionResult);
            return new WorkflowRunResult(_payloadConverter,
                                         _namespace,
                                         _workflowId,
                                         _workflowChainId,
                                         workflowRunId,
                                         WorkflowExecutionStatus.ContinuedAsNew,
                                         await DecodePayloads(eventAttributes.LastCompletionResult, cancelToken),
                                         failure: TemporalFailure.FromMessage(eventAttributes.Failure),
                                         continuationRunId: eventAttributes.NewExecutionRunId,
                                         eventAttributes);
        }

        private Task<Payloads> DecodePayloads(Payloads encodedPayloads, CancellationToken cancelToken)
        {
            return (_payloadCodec != null && encodedPayloads != null)
                            ? _payloadCodec.DecodeAsync(encodedPayloads, cancelToken)
                            : Task.FromResult(encodedPayloads);
        }
    }
}
