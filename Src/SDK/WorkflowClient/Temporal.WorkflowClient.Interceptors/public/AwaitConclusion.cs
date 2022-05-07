using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Temporal.Util;
using Temporal.Api.Common.V1;
using Temporal.Api.Enums.V1;
using Temporal.Common;
using Temporal.Common.Payloads;
using Temporal.Serialization;
using Temporal.WorkflowClient.Errors;

namespace Temporal.WorkflowClient.Interceptors
{
    public static class AwaitConclusion
    {
        public record Arguments(string Namespace,
                                string WorkflowId,
                                string WorkflowChainId,
                                string WorkflowRunId,
                                bool FollowWorkflowChain,
                                CancellationToken CancelToken) : IWorkflowOperationArguments;

        internal class Result : IWorkflowRunResult, IWorkflowOperationResult
        {
            private readonly IPayloadConverter _payloadConverter;
            private readonly string _continuationRunId;
            private readonly string _workflowChainId;
            private readonly Payloads _serializedPayloads;

            public Result(IPayloadConverter payloadConverter,
                          string @namespace,
                          string workflowId,
                          string workflowChainId,
                          string workflowRunId,
                          WorkflowExecutionStatus status,
                          Payloads serializedPayloads,
                          Exception failure,
                          string continuationRunId,
                          object conclusionEventAttributes)
            {
                Validate.NotNull(payloadConverter);
                _payloadConverter = payloadConverter;

                Validate.NotNullOrWhitespace(@namespace);
                Namespace = @namespace;

                ValidateWorkflowProperty.WorkflowId(workflowId);
                WorkflowId = workflowId;

                ValidateWorkflowProperty.ChainId.Bound(workflowChainId);
                _workflowChainId = workflowChainId;

                // @ToDo: Once server supports it, change to 
                // ValidateWorkflowProperty.RunId.Specified(workflowRunId);
                ValidateWorkflowProperty.RunId.SpecifiedOrUnspecified(workflowRunId);
                WorkflowRunId = workflowRunId;

                Status = status;
                Failure = failure;

                _serializedPayloads = serializedPayloads; // may be null

                ValidateWorkflowProperty.RunId.SpecifiedOrUnspecified(continuationRunId);
                _continuationRunId = continuationRunId;

                ConclusionEventAttributes = conclusionEventAttributes;

                WorkflowChain = null;
            }

            public string Namespace { get; }

            public string WorkflowId { get; }

            public string WorkflowRunId { get; }

            public bool IsConcludedSuccessfully { get { return (Status == WorkflowExecutionStatus.Completed); } }

            public bool TryGetBoundWorkflowChainId(out string workflowChainId)
            {
                workflowChainId = _workflowChainId;
                return (workflowChainId != null);
            }

            public WorkflowExecutionStatus Status { get; }

            public Exception Failure { get; }

            public bool IsContinuedAsNew { get { return (_continuationRunId != null); } }

            internal IWorkflowHandle WorkflowChain { get; set; }

            public bool TryGetContinuationRunId(out string continuationRunId)
            {
                continuationRunId = _continuationRunId;
                return (continuationRunId != null);
            }

            public bool TryGetContinuationRun(out IWorkflowRunHandle continuationRunHandle)
            {
                if (TryGetContinuationRunId(out string continuationRunId) && WorkflowChain != null)
                {
                    continuationRunHandle = WorkflowChain.CreateRunHandle(continuationRunId);
                    return true;
                }

                continuationRunHandle = null;
                return false;
            }

            public object ConclusionEventAttributes { get; }

            /// <summary>Throws for any Status except OK. This method backs GetResult(..) on WorkflowHandle.</summary>        
            [SuppressMessage("Style", "IDE0010:Add missing cases", Justification = "Switch on Status groups all non-success cases")]
            public TVal GetValue<TVal>()
            {
                switch (Status)
                {
                    case WorkflowExecutionStatus.Completed:
                    case WorkflowExecutionStatus.ContinuedAsNew:
                    {
                        if (_serializedPayloads != null)
                        {
                            return GetPayload<TVal>();
                        }

                        // If no `_payloadConverter` is specified, we can get the payload value of type `IPayload.Void`.
                        if (Temporal.Common.Payload.Void.TryCast<IPayload.Void, TVal>(out TVal value))
                        {
                            return value;
                        }


                        throw new MalformedServerResponseException(serverCall: null,
                                                                   scenario: null,
                                                                   $"Cannot {nameof(GetValue)}<{typeof(TVal).Name}>() because the remote"
                                                                 + $" operation represented by this {nameof(IWorkflowRunResult)}-instance"
                                                                 + $" returned no appropriate result payload (conclusion status: {Status}).");
                    }

                    default:
                    {
                        throw new WorkflowConcludedAbnormallyException($"Cannot {nameof(GetValue)}<{typeof(TVal).Name}>() because the remote"
                                                                     + $" workflow represented by this {nameof(IWorkflowRunResult)}-instance"
                                                                     + $" did not conclude successfully.",
                                                                       Status,
                                                                       Namespace,
                                                                       WorkflowId,
                                                                       _workflowChainId,
                                                                       WorkflowRunId,
                                                                       Failure);
                    }
                }
            }

            public PayloadContainers.IUnnamed GetValue()
            {
                return GetValue<PayloadContainers.Unnamed.SerializedDataBacked>();
            }

            /// <summary>
            /// Doesn't throw on non-OK Status. Can be used to retrieve payloads that came with non-OK (aka non-Completed) terminal events.
            /// </summary>
            public TVal GetPayload<TVal>()
            {
                if (_serializedPayloads != null)
                {
                    return PayloadConverter.Deserialize<TVal>(_payloadConverter, _serializedPayloads);
                }

                // If no `_payloadConverter` is specified, we can get the payload value of type `IPayload.Void`.
                if (Temporal.Common.Payload.Void.TryCast<IPayload.Void, TVal>(out TVal value))
                {
                    return value;
                }

                throw new InvalidOperationException($"Cannot {nameof(GetValue)}<{typeof(TVal).Name}>() because this {this.GetType().Name}"
                                                  + $" does not represent any serialized value payloads.");
            }

            public PayloadContainers.IUnnamed GetPayload()
            {
                return GetPayload<PayloadContainers.IUnnamed>();
            }

            /// <summary>
            /// Doesn't throw on non-OK Status. Can be used to retrieve payloads that came with non-OK (aka non-Completed) terminal events.
            /// </summary>
            public bool TryGetPayload<TVal>(out TVal deserializedPayload)
            {
                if (_serializedPayloads == null)
                {
                    deserializedPayload = default(TVal);
                    return false;
                }

                return _payloadConverter.TryDeserialize<TVal>(_serializedPayloads, out deserializedPayload);
            }

            public bool TryGetPayload(out PayloadContainers.IUnnamed deserializedPayload)
            {
                bool canGet = TryGetPayload(out PayloadContainers.Unnamed.SerializedDataBacked container);
                deserializedPayload = container;
                return canGet;
            }
        }
    }
}
