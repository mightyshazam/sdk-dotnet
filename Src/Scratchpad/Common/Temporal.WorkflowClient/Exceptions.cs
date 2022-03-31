using System;
using Temporal.Common.Exceptions;

namespace Temporal.WorkflowClient
{
    /// <summary>
    /// When an exception propagates from a worker or from the Temporal server to a client,
    /// it is always wrapped into an instance of this class.
    /// The stack trace of this exception represents the local stack trace on the client.
    /// The InnerException represents the failure on the worker (or the server) and its stack trace describes the worker-side stack.
    /// <br /><br />
    /// Exceptions that originate in the workflow client, but are NOT caused by a remote issue are NOT wrapped into instances
    /// of this class (e.g. connection issues, local cancellations, etc.)
    /// </summary>
    public sealed class RemoteTemporalException : Exception
    {        
        private static Exception AsException(ITemporalFailure failure)
        {
            if (failure == null)
            {
                throw new ArgumentNullException(nameof(failure));
            }

            if (failure is not Exception exception)
            {
                throw new ArgumentException($"The type of the specified instance of {nameof(ITemporalFailure)} must"
                                          + $" be a subclass of {nameof(Exception)}, but it is not the case for the actual"
                                          + $" runtime type (\"{failure.GetType().FullName}\").", nameof(failure));
            }

            return exception;
        }

#if NET6_0_OR_GREATER
        [System.Diagnostics.StackTraceHidden]
#endif
        public static RemoteTemporalException Throw(string message, ITemporalFailure innerException)
        {
            throw new RemoteTemporalException(message, innerException);
        }

        public RemoteTemporalException(string message, ITemporalFailure innerException)
            : base(message, AsException(innerException))
        {
            Cause = innerException;
        }

        public ITemporalFailure Cause { get; }
    }
}