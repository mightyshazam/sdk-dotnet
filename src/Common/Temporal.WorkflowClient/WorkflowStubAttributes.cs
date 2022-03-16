using System;
using System.ComponentModel;

namespace Temporal.WorkflowClient
{
    public class WorkflowStubAttributes
    {
    }

    public enum WorkflowMainMethodStubInvocationPolicy
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        Unspecified = WorkflowChainBindingPolicy.Unspecified,

        /// <summary>
        /// If a Main Method Stub attributed with this policy is invoked on a stub instance that is ALREADY BOUND to a particular chain:
        ///   Then the method stub will return a Task representing the completion of that chain (which may or may not already be completed).
        ///   The main routine will not be invoked again. Method arguments, if any, will be ignored.
        /// If a Main Method Stub attributed with this policy is invoked on a stub instance that is NOT YET BOUND to a particular chain:
        ///   Start-New-Workflow gRPC API will be invoked to start a new chain.
        ///   If Start succeeds, the stub will be bound to the new chain and the returned Task will represent the chan's completion.
        ///   If Start fails with "workflow-already-exists", use Descripe gRPC API to get the latest chain, bind to that chain
        ///   and return a Task representing that chains completion.
        ///   If Start fails with other errors, then propagate the failure.
        /// </summary>
        StartNewOrGetResult = WorkflowChainBindingPolicy.NewOrLatestChain,

        /// <summary>
        /// If a Main Method Stub attributed with this policy is invoked on a stub instance that is ALREADY BOUND to a particular chain:
        ///   Then the method stub will return a Task representing the completion of that chain (which may or may not already be completed).
        ///   The main routine will not be invoked again. Method arguments, if any, will be ignored.
        /// If a Main Method Stub attributed with this policy is invoked on a stub instance that is NOT YET BOUND to a particular chain:
        ///   Use Descripe gRPC API to get the latest chain, bind to that chain and return a Task representing that chains completion.
        ///   Method arguments, if any, will be ignored.        
        /// </summary>
        GetResult = WorkflowChainBindingPolicy.LatestChain,

        /// <summary>
        /// If a Main Method Stub attributed with this policy is invoked on a stub instance that is ALREADY BOUND to a particular chain:
        ///   Throw Invalid Operation. 
        /// If a Main Method Stub attributed with this policy is invoked on a stub instance that is NOT YET BOUND to a particular chain:
        ///   Start-New-Workflow gRPC API will be invoked to start a new chain.
        ///   If Start succeeds, the stub will be bound to the new chain and the returned Task will represent the chan's completion.
        ///   If Start fails with "workflow-already-exists", use Descripe gRPC API to get the latest chain, bind to that chain
        ///   and return a Task representing that chains completion.
        ///   If Start fails with other errors, then propagate the failure.
        /// </summary>
        StartNew = WorkflowChainBindingPolicy.NewChainOnly
    }

    /// <summary>
    /// Specifies that invocations of the marked method are forwarded to the main routine of the remote workflow
    /// chain bound to the stub.
    /// This attribute can be applied to methods with these signatures:
    ///     Task SomeMethod()
    ///     Task{TResult} SomeMethod() where TResult : IDataValue
    ///     Task SomeMethod(TArg args) where TArg : IDataValue
    ///     Task{TResult} SomeMethod(TArg args) where TArg : IDataValue where TResult : IDataValue
    /// otherwise an error during stub generation is thrown.
    /// 
    /// The workflow type name is specified when invoking the API that generates the stub. It is not specified here.
    /// 
    /// It is NOT prohibited to have MULTIPLE stub methods point to the main workflow method.
    /// 
    /// Parameters are not validated by the client and are sent to the workflow as provided.
    /// If no parameters are provided, an empty payload is sent.
    /// 
    /// When a WorkflowMainMethodStub is invoked, several things (or may not) happen:
    ///  - Starting a new workflow chain and thus invoking the workflow's main routine;
    ///  - Binding the workflow stub to a particular chain;
    ///  - Returning a Task that represents the eventual completion of the bound workflow chain.
    ///  
    /// Which of these things happen, and in what order is defined by the specified
    /// <see cref="WorkflowMainMethodStubInvocationPolicy"/>.
    /// 
    /// This and other 'WorkflowXxxStub' attributes can only be applied to method definitions in interfaces.
    /// They are ignored in classes.
    /// 
    /// This and other 'WorkflowXxxStub' are interpreted by the workflow client SDK. They do NOT configure how
    /// workflow implementations are hosted by the worker. However, if a worker host loads a workflow that
    /// implements interfaces with 'WorkflowXxxStub'-attributed methods, the host will validate that the workflow
    /// implementation has attribute-based handlers for APIs defined by the 'WorkflowXxxStub' attributes.
    /// 
    /// Note that the signatures required for a 'WorkflowXxxStubAttribute' may be different from the corresponding
    /// worker-side 'WorkflowXxxHandlerAttribute'.
    /// For example, queries are async from the client perspective, but must be handled synchronously in the implementation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public sealed class WorkflowMainMethodStubAttribute : Attribute
    {
        public WorkflowMainMethodStubInvocationPolicy InvocationPolicy { get; }

        public WorkflowMainMethodStubAttribute(WorkflowMainMethodStubInvocationPolicy invocationPolicy)
        {
            InvocationPolicy = invocationPolicy;
        }
    }

    /// <summary>
    /// Can be applied to methods with signatures:
    ///     Task SomeMethod()
    ///     Task SomeMethod(CancellationToken cancelToken)
    ///     
    ///     Task SomeMethod(TArg args) where TArg : IDataValue
    ///     Task SomeMethod(TArg args, CancellationToken cancelToken) where TArg : IDataValue
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
    /// See <see cref="WorkflowMainMethodStubAttribute" /> for more information on various stub methods and their relation to handler methods.
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
    /// See <see cref="WorkflowMainMethodStubStubAttribute" /> for more information on various stub methods and their relation to handler methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public sealed class WorkflowQueryStubAttribute : Attribute
{
    public string QueryTypeName { get; set; }
    public WorkflowQueryStubAttribute() { }
}
}
