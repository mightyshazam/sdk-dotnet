using System;

namespace Temporal.WorkflowClient
{
    public class WorkflowStubAttributes
    {
    }

    /// <summary>
    /// Can be applied to methods with signatures:
    ///     Task SomeMethod()
    ///     Task{TResult} SomeMethod() where TResult : IDataValue
    ///     Task SomeMethod(TArg args) where TArg : IDataValue
    ///     Task{TResult} SomeMethod(TArg args) where TArg : IDataValue where TResult : IDataValue
    ///     Task{PayloadsCollection} SomeMethod(PayloadsCollection args)
    /// otherwise an error during stub generation is thrown.
    /// @ToDo: Cancellation Tokens?
    /// 
    /// The workflow type name is specified to the API that generated the stub. It is not specified here.
    /// 
    /// It is NOT prohibited to have MULTIPLE stub methods point to the main workflow method.
    /// Parameters are not validated by the client and are sent to the workflow as provided.
    /// If no parameters are provided, an empty payload is sent.
    /// 
    /// If a RunMethodStub is invoked on a stub instance that is NOT yet bound to a workflow run, it will attempt to bind the stub
    /// instance to the first option permitted by both, the 'WorkflowRunStubConfiguration' specified to the stab creation method and
    /// the settings of this attribute in the following order:
    ///   1) Bind to an existing Active run
    ///   2) Start a New run and bind to it
    ///   3) Bind to an existing Finished run
    /// If it cannot find anything to bind to based on above-mentioned permissions, an error is thrown.
    /// If permissions specify a binding strategy, but the execution fails, the respective error is thrown
    /// and no other binding is attempted. For example, assume that all CanBindXxx settings are True, and there are existing runs,
    /// yet all of them are finished. In that case, based on the above order, it will try to start a New run and bind to it.
    /// If, however, starting new run fails based on the 'WorkflowIdReusePolicy', the failure will be propagated and binding
    /// to an existing Finished run will not be attempted.
    /// 
    /// If CanBindToExistingRun is true, then the RunMethodStub can be called for an active workflow run multiple times.
    /// In that case the returned Task represents the completion of the active run; the run is not started again.
    /// Potential arguments are ignored in such case.
    /// 
    /// If the 'MustBindToNewIfContinued' property of the 'WorkflowRunStubConfiguration' used when the stub instance was created is True,
    /// then the Task returned by a RunMethodStub represents the completion of the ENTIRE workflow, including the as-new continuations
    /// of the current run.
    /// 
    /// This and other 'WorkflowXxxStub' attributes can only be applied to method definitions in interfaces.
    /// They are ignored in classes.
    /// This and other 'WorkflowXxxStub' are interpreted by the workflow client SDK. They do NOT configure how
    /// workflow implementations are hosted by the worker. However, if a worker host loads a workflow that implements interfaces
    /// with 'WorkflowXxxStub'-attributed methods, the host will validate that the workflow implementation has attribute-based
    /// handlers for APIs defined by the 'WorkflowXxxStub' attributes.
    /// 
    /// Note that the signatures required for a 'WorkflowXxxStubAttribute' may be different from the corresponding 'WorkflowXxxHandlerAttribute'.
    /// For examples, queries are async from the client perspective, but must be handled synchronously in the implementation.
    ///     
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public sealed class WorkflowRunMethodStubAttribute : Attribute
    {
        public bool CanBindToNewRun { get; set; }
        public bool CanBindToExistingRun { get; set; }
        public WorkflowRunMethodStubAttribute() { CanBindToNewRun = true; CanBindToExistingRun = true; }
    }

    /// <summary>
    /// Can be applied to methods with signatures:
    ///     Task SomeMethod()
    ///     Task SomeMethod(CancellationToken cancelToken)
    ///     
    ///     Task SomeMethod(TArg args) where TArg : IDataValue
    ///     Task SomeMethod(TArg args, CancellationToken cancelToken) where TArg : IDataValue
    ///     
    ///     Task SomeMethod(PayloadsCollection args)
    ///     Task SomeMethod(PayloadsCollection args, CancellationToken cancelToken)
    /// otherwise an error during stub generation is thrown.
    /// 
    /// If 'SignalTypeName' is not specified OR null OR Empty OR WhiteSpaceOnly, then the signal type name is auto-populated
    /// by taking the method name and removing 'Async' from its end if present
    /// (if that would result in an empty string, then 'Async' is not removed).
    /// 
    /// It is NOT prohibited to have multiple stub methods point to the same signal.
    /// Parameters are not validated by the client and are sent to the workflow signal handler as provided.
    /// 
    /// If the stub instance is not bound when a SignalStub-method is invoked, an error is thrown.
    /// 
    /// See <see cref="WorkflowRunMethodStubAttribute" /> for more information on various stub methods and their relation to handler methods.
    /// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public sealed class WorkflowSignalStubAttribute : Attribute
{
    public string SignalTypeName { get; set; }
    public WorkflowSignalStubAttribute() { }
}

    /// <summary>
    /// Can be applied to methods with signatures:
    ///     Task{TResult} SomeMethod() where TResult : IDataValue
    ///     Task{TResult} SomeMethod(CancellationToken cancelToken) where TResult : IDataValue
    ///     
    ///     Task{TResult} SomeMethod(TArg args) where TArg : IDataValue where TResult : IDataValue
    ///     Task{TResult} SomeMethod(TArg args, CancellationToken cancelToken) where TArg : IDataValue where TResult : IDataValue
    ///     
    ///     Task{PayloadsCollection} SomeMethod(PayloadsCollection args)
    ///     Task{PayloadsCollection} SomeMethod(PayloadsCollection args, CancellationToken cancelToken)
    /// otherwise an error during stub generation is thrown.
    /// 
    /// If 'QueryTypeName' is not specified OR null OR Empty OR WhiteSpaceOnly, then the query type name is auto-populated
    /// by taking the method name and removing 'Async' from its end if present
    /// (if that would result in an empty string, then 'Async' is not removed).
    /// 
    /// It is NOT prohibited to have multiple stub methods point to the same query.
    /// Parameters are not validated by the client and are sent to the workflow query handler as provided.
    /// 
    /// If the stub instance is not bound when a QueryStub-method is invoked, an error is thrown.
    /// 
    /// See <see cref="WorkflowRunMethodStubAttribute" /> for more information on various stub methods and their relation to handler methods.
    /// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public sealed class WorkflowQueryStubAttribute : Attribute
{
    public string QueryTypeName { get; set; }
    public WorkflowQueryStubAttribute() { }
}
}
