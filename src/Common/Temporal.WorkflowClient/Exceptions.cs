using System;

namespace Temporal.WorkflowClient
{
    /// <summary>
    /// When an exception propagates from a worker or from the Temporal server to a client,
    /// it is always wrapped into an instance of this class.
    /// The stack trace of this exception represents the local stack trace on the client.
    /// The InnerException represetns the failure on the worker (or the server) and its stack trace describes the worker-side stack.
    /// <br /><br />
    /// Exceptions that originate in the workflow client, but are NOT caused by a remote issue are NOT wrapped into instances
    /// of this class (e.g. connection issues, local cancellations, etc.)
    /// </summary>
    public sealed class RemoteTemporalException : Exception
    {        
        internal RemoteTemporalException(string message, Exception innerException) : base(message, innerException) { }
    }
}