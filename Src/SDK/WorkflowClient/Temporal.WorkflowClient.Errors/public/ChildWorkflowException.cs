using System;
using System.Runtime.Serialization;
using Candidly.Util;
using Temporal.Api.Enums.V1;

namespace Temporal.WorkflowClient.Errors
{
    public sealed class ChildWorkflowException : Exception, ITemporalFailure
    {
        public string Namespace { get; }
        public string WorkflowId { get; }
        public string WorkflowRunId { get; }
        public string WorkflowTypeName { get; }
        public long InitiatedEventId { get; }
        public long StartedEventId { get; }
        public RetryState RetryState { get; }

        public ChildWorkflowException(string message,
                                      string @namespace,
                                      string workflowId,
                                      string workflowRunId,
                                      string workflowTypeName,
                                      long initiatedEventId,
                                      long startedEventId,
                                      RetryState retryState,
                                      Exception innerException)
            : base(Format.TrimSafe(message), innerException)
        {
            Namespace = Format.TrimSafe(@namespace);
            WorkflowId = Format.TrimSafe(workflowId);
            WorkflowRunId = Format.TrimSafe(workflowRunId);
            WorkflowTypeName = Format.TrimSafe(workflowTypeName);
            InitiatedEventId = initiatedEventId;
            StartedEventId = startedEventId;
            RetryState = retryState;
        }

        internal ChildWorkflowException(SerializationInfo info, StreamingContext context)
                : base(info, context)
        {
            Namespace = Format.TrimSafe(info.GetString(nameof(Namespace)));
            WorkflowId = Format.TrimSafe(info.GetString(nameof(WorkflowId)));
            WorkflowRunId = Format.TrimSafe(info.GetString(nameof(WorkflowRunId)));
            WorkflowTypeName = Format.TrimSafe(info.GetString(nameof(WorkflowTypeName)));
            InitiatedEventId = info.GetInt64(nameof(InitiatedEventId));
            StartedEventId = info.GetInt64(nameof(StartedEventId));
            RetryState = (RetryState) info.GetInt32(nameof(RetryState));
        }
    }
}
