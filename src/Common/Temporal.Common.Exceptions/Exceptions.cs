﻿using System;
using Temporal.Common.DataModel;


namespace Temporal.Common.Exceptions
{    
    /// <summary>
    /// Marker iface for exceptions that can propagate from a worker to a workflow client.
    /// We do not use a class as where is no common actor functionality and we do not want to prohibit specific
    /// exceptions from subclassing specific semantic exceptions types in the future.
    /// </summary>
    public interface IWorkflowException
    {
    }
    
    public sealed class ApplicationException : Exception, IWorkflowException
    {
        public bool IsNonRetryable { get; }
        public ApplicationException(string message) : this(message, isNonRetryable: false) { }
        public ApplicationException(string message, Exception innerException) : this(message, innerException, isNonRetryable: false) { }
        public ApplicationException(string message, bool isNonRetryable) : base(message) { IsNonRetryable = isNonRetryable; }
        public ApplicationException(string message, Exception innerException, bool isNonRetryable) : base(message, innerException) { IsNonRetryable = isNonRetryable; }
    }

    public sealed class TimeoutException : Exception, IWorkflowException
    {
        public TimeoutType TimeoutType { get; }        
        public TimeoutException(string message, TimeoutType timeoutType) : base(message) { TimeoutType = timeoutType; }
        public TimeoutException(string message, Exception innerException, TimeoutType timeoutType) : base(message, innerException) { TimeoutType = timeoutType; }
    }

    public sealed class CancellationException : Exception, IWorkflowException
    {
        public CancellationException(string message) : base(message) { }
        public CancellationException(string message, Exception innerException) : base(message, innerException) { }
    }

    public sealed class TerminationException : Exception, IWorkflowException
    {
        public TerminationException(string message) : base(message) { }
        public TerminationException(string message, Exception innerException) : base(message, innerException) { }
    }

    public sealed class OrchesrationServerException : Exception, IWorkflowException
    {
        public bool IsNonRetryable { get; }
        public OrchesrationServerException(string message) : this(message, isNonRetryable: false) { }
        public OrchesrationServerException(string message, Exception innerException) : this(message, innerException, isNonRetryable: false) { }
        public OrchesrationServerException(string message, bool isNonRetryable) : base(message) { IsNonRetryable = isNonRetryable; }
        public OrchesrationServerException(string message, Exception innerException, bool isNonRetryable) : base(message, innerException) { IsNonRetryable = isNonRetryable; }
    }

    public sealed class ActivityException : Exception, IWorkflowException
    {
        public ActivityException(string message) : base(message) { }
        public ActivityException(string message, Exception innerException) : base(message, innerException) { }
        // long scheduled_event_id, long started_event_id, string identity,
        // string activityTypeName, string activity_id, RetryState retry_state
    }

    public sealed class ChildWorkflowException : Exception, IWorkflowException
    {
        public ChildWorkflowException(string message) : base(message) { }
        public ChildWorkflowException(string message, Exception innerException) : base(message, innerException) { }
        // string namespace; WorkflowExecution workflow_execution; WorkflowType workflow_type; long initiated_event_id;
        // long started_event_id = 5; RetryState retry_state = 6;
    }
}
