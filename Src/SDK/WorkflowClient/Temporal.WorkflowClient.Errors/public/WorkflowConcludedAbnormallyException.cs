using System;
using Candidly.Util;
using Temporal.Api.Enums.V1;

namespace Temporal.WorkflowClient.Errors
{
    public sealed class WorkflowConcludedAbnormallyException : Exception
    {
        private static Exception AsException(ITemporalFailure failure)
        {
            if (failure == null)
            {
                return null;
            }

            if (failure is not Exception exception)
            {
                throw new ArgumentException($"The type of the specified instance of {nameof(ITemporalFailure)} must"
                                          + $" be a subclass of {nameof(Exception)}, but it is not the case for the"
                                          + $" actual runtime type (\"{failure.GetType().FullName}\").",
                                            nameof(failure));
            }

            return exception;
        }

        private static ITemporalFailure AsTemporalFailure(Exception exception)
        {
            if (exception == null)
            {
                return null;
            }

            if (exception is not ITemporalFailure failure)
            {
                throw new ArgumentException($"The type of the specified instance of {nameof(Exception)} must"
                                          + $" implement the interface {nameof(ITemporalFailure)}, but it is not the case for the"
                                          + $" actual runtime type (\"{exception.GetType().FullName}\").",
                                            nameof(exception));
            }

            return failure;
        }

        private static string FormatMessage(string message,
                                            WorkflowExecutionStatus conclusionStatus,
                                            string @namespace,
                                            string workflowId,
                                            string workflowChainId,
                                            string workflowRunId,
                                            ITemporalFailure innerException)
        {
            message = String.IsNullOrWhiteSpace(message) ? nameof(WorkflowConcludedAbnormallyException) : message.Trim();

            if (!message.EndsWith("."))
            {
                message = message + ".";
            }

            if (innerException != null)
            {
                message = message + " Inner Exception may have additional details.";
            }

            message = $"{message} (ConclusionStatus='{conclusionStatus}'; Namespace={@namespace.QuoteOrNull()};"
                    + $" WorkflowId={workflowId.QuoteOrNull()}; WorkflowChainId={workflowChainId.QuoteOrNull()};"
                    + $" WorkflowRunId={workflowRunId.QuoteOrNull()})";

            return message;
        }

        public WorkflowConcludedAbnormallyException(string message,
                                                    WorkflowExecutionStatus conclusionStatus,
                                                    string @namespace,
                                                    string workflowId,
                                                    string workflowChainId,
                                                    string workflowRunId,
                                                    Exception innerException)
            : this(message, conclusionStatus, @namespace, workflowId, workflowChainId, workflowRunId, AsTemporalFailure(innerException))
        {
        }

        public WorkflowConcludedAbnormallyException(string message,
                                                    WorkflowExecutionStatus conclusionStatus,
                                                    string @namespace,
                                                    string workflowId,
                                                    string workflowChainId,
                                                    string workflowRunId,
                                                    ITemporalFailure innerException)
            : base(FormatMessage(message, conclusionStatus, @namespace, workflowId, workflowChainId, workflowRunId, innerException),
                   AsException(innerException))
        {
            ConclusionStatus = conclusionStatus;
            Namespace = @namespace;
            WorkflowId = workflowId;
            WorkflowChainId = workflowChainId;
            WorkflowRunId = workflowRunId;
        }

        public WorkflowExecutionStatus ConclusionStatus { get; }
        public string Namespace { get; }
        public string WorkflowId { get; }
        public string WorkflowChainId { get; }
        public string WorkflowRunId { get; }
    }
}
