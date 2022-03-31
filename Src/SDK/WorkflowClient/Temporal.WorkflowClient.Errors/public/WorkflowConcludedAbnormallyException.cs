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
                                            string @namespace,
                                            string workflowId,
                                            string workflowChainId,
                                            string workflowRunId,
                                            WorkflowExecutionStatus conclusionStatus,
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

            message = $"{message} (@namespace={@namespace.QuoteOrNull()}; workflowId={workflowId.QuoteOrNull()};"
                    + $" workflowChainId={workflowChainId.QuoteOrNull()}; workflowRunId={workflowRunId.QuoteOrNull()};"
                    + $" conclusionStatus='{conclusionStatus}')";

            return message;
        }

        public WorkflowConcludedAbnormallyException(string message,
                                                    string @namespace,
                                                    string workflowId,
                                                    string workflowChainId,
                                                    string workflowRunId,
                                                    WorkflowExecutionStatus conclusionStatus,
                                                    Exception innerException)
            : this(message, @namespace, workflowId, workflowChainId, workflowRunId, conclusionStatus, AsTemporalFailure(innerException))
        {
        }

        public WorkflowConcludedAbnormallyException(string message,
                                                    string @namespace,
                                                    string workflowId,
                                                    string workflowChainId,
                                                    string workflowRunId,
                                                    WorkflowExecutionStatus conclusionStatus,
                                                    ITemporalFailure innerException)
            : base(FormatMessage(message, @namespace, workflowId, workflowChainId, workflowRunId, conclusionStatus, innerException),
                   AsException(innerException))
        {
            Namespace = @namespace;
            WorkflowId = workflowId;
            WorkflowChainId = workflowChainId;
            WorkflowRunId = workflowRunId;
            ConclusionStatus = conclusionStatus;
        }

        public string Namespace { get; }
        public string WorkflowId { get; }
        public string WorkflowChainId { get; }
        public string WorkflowRunId { get; }
        public WorkflowExecutionStatus ConclusionStatus { get; }
    }
}
