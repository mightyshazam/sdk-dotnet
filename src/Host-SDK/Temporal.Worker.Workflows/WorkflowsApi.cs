using System;
using System.Threading;
using System.Threading.Tasks;

using Temporal.Common.WorkflowConfiguration;
using Temporal.Common.DataModel;
using Temporal.Serialization;
using Temporal.Worker.Workflows.Dynamic;

namespace Temporal.Worker.Workflows
{
    public class WorkflowsApi
    {
    }

    // ----------- -----------

    public interface IWorkflowContext
    {        
        IWorkflowExecutionConfiguration WorkflowExecutionConfig { get; }
        IWorkflowImplementationConfiguration WorkflowImplementationConfig { get; }
        WorkflowRunContext CurrentRun { get; }
        WorkflowPreviousRunContext  LastRun { get; }
        IDeterministicApi DeterministicApi { get; }

        IActivityOrchestrationService Activities { get; }
        void ConfigureContinueAsNew(bool startNewRunAfterReturn, IDataValue newRunInput);
        void ConfigureContinueAsNew(bool startNewRunAfterReturn);
        Task SleepAsync(TimeSpan timeSpan);
        Task<bool> SleepAsync(TimeSpan timeSpan, CancellationToken cancelToken);
        Task SleepUntilAsync(DateTime sleepEndUtc);
        Task<bool> SleepUntilAsync(DateTime sleepEndUtc, CancellationToken cancelToken);

        IPayloadSerializer GetSerializer(PayloadsCollection payloads);
        IPayloadSerializer GetSerializer();
    }

    internal class WorkflowContext : IWorkflowContext, IDynamicWorkflowContext
    {
        public IWorkflowExecutionConfiguration WorkflowExecutionConfig { get; }
        public IWorkflowImplementationConfiguration WorkflowImplementationConfig { get; }
        public WorkflowRunContext CurrentRun { get; }
        public WorkflowPreviousRunContext LastRun { get; }
        public IDeterministicApi DeterministicApi { get; }

        public IDynamicWorkflowController DynamicControl { get; }

        public IActivityOrchestrationService Activities { get; }
        public void ConfigureContinueAsNew(bool startNewRunAfterReturn, IDataValue newRunInput) { }
        public void ConfigureContinueAsNew(bool startNewRunAfterReturn) { }
        public Task SleepAsync(TimeSpan timeSpan) { return null; }
        public Task<bool> SleepAsync(TimeSpan timeSpan, CancellationToken cancelToken) { return null; }
        public Task SleepUntilAsync(DateTime sleepEndUtc) { return null; }
        public Task<bool> SleepUntilAsync(DateTime sleepEndUtc, CancellationToken cancelToken) { return null; }

        /// <summary>Get the serializer for the specified payload.
        /// If metadata specifies an available serializer - get that one;
        /// If metadata specifies an unavailable serializer - throw;
        /// If metadata specifies nothing - get the default form the config.
        /// If nothing configured - get JSON.</summary>        
        public IPayloadSerializer GetSerializer(PayloadsCollection payloads) { return null; }
        public IPayloadSerializer GetSerializer() { return null; }
    }

    // ----------- -----------

    public interface IActivityOrchestrationService
    {
        Task<PayloadsCollection> ExecuteAsync(string activityName, PayloadsCollection activityArguments);
        Task<PayloadsCollection> ExecuteAsync(string activityName, PayloadsCollection activityArguments, CancellationToken cancelToken);
        Task<PayloadsCollection> ExecuteAsync(string activityName, PayloadsCollection activityArguments, IActivityInvocationConfiguration invocationConfig);
        Task<PayloadsCollection> ExecuteAsync(string activityName, PayloadsCollection activityArguments, CancellationToken cancelToken, IActivityInvocationConfiguration invocationConfig);

        Task ExecuteAsync(string activityName);
        Task ExecuteAsync(string activityName, CancellationToken cancelToken);
        Task ExecuteAsync(string activityName, IActivityInvocationConfiguration invocationConfig);
        Task ExecuteAsync(string activityName, CancellationToken cancelToken, IActivityInvocationConfiguration invocationConfig);

        Task ExecuteAsync<TArg>(string activityName, TArg activityArguments) where TArg : IDataValue;
        Task ExecuteAsync<TArg>(string activityName, TArg activityArguments, CancellationToken cancelToken) where TArg : IDataValue;
        Task ExecuteAsync<TArg>(string activityName, TArg activityArguments, IActivityInvocationConfiguration invocationConfig) where TArg : IDataValue;
        Task ExecuteAsync<TArg>(string activityName, TArg activityArguments, CancellationToken cancelToken, IActivityInvocationConfiguration invocationConfig) where TArg : IDataValue;

        Task<TResult> ExecuteAsync<TResult>(string activityName) where TResult : IDataValue;
        Task<TResult> ExecuteAsync<TResult>(string activityName, CancellationToken cancelToken) where TResult : IDataValue;
        Task<TResult> ExecuteAsync<TResult>(string activityName, IActivityInvocationConfiguration invocationConfig) where TResult : IDataValue;
        Task<TResult> ExecuteAsync<TResult>(string activityName, CancellationToken cancelToken, IActivityInvocationConfiguration invocationConfig) where TResult : IDataValue;

        Task<TResult> ExecuteAsync<TArg, TResult>(string activityName, TArg activityArguments) where TArg : IDataValue where TResult : IDataValue;
        Task<TResult> ExecuteAsync<TArg, TResult>(string activityName, TArg activityArguments, CancellationToken cancelToken) where TArg : IDataValue where TResult : IDataValue;
        Task<TResult> ExecuteAsync<TArg, TResult>(string activityName, TArg activityArguments, IActivityInvocationConfiguration invocationConfig) where TArg : IDataValue where TResult : IDataValue;
        Task<TResult> ExecuteAsync<TArg, TResult>(string activityName, TArg activityArguments, CancellationToken cancelToken, IActivityInvocationConfiguration invocationConfig) where TArg : IDataValue where TResult : IDataValue;
    }

    // ----------- -----------

    public interface IDeterministicApi
    {
        DateTime DateTimeUtcNow { get; }

        Random CreateNewRandom();
        Guid CreateNewGuid();
        CancellationTokenSource CreateNewCancellationTokenSource();
        CancellationTokenSource CreateNewCancellationTokenSource(TimeSpan delay);
    }

    internal class DeterministicApi : IDeterministicApi
    {
        public DateTime DateTimeUtcNow { get; }

        public Random CreateNewRandom() { return null; }

        public Guid CreateNewGuid() { return default(Guid); }

        public CancellationTokenSource CreateNewCancellationTokenSource() { return null; }
        public CancellationTokenSource CreateNewCancellationTokenSource(TimeSpan delay) { return null; }
}

    // ----------- -----------

    public class WorkflowRunContext
    {
        public CancellationToken CancelToken { get; }
        public string RunId { get; }
        public PayloadsCollection Input { get; }
        
    }

    public class WorkflowPreviousRunContext
    {
        public bool IsAvailable { get; }
        public Task<TResult> TryGetCompletion<TResult>() { return null; }
        public Task TryGetCompletion() { return null; }
        public Task<PayloadsCollection> GetCompletion() { return null; }
    }

    // ----------- -----------

    
    /// <summary>
    /// Per-workflow settings related to the business logic and/or the execution container of a workflow.
    /// May affect the business logic.
    /// These settings are set on the worker/host (globally or for a specific workflow), not by the client invoking the workflow.
    /// If set in on several layers/levels in the host, these settings will be merged before being applied to a specific workflow
    /// at the time of starting it. Once started, they need to be read-only.    
    /// Example: Serializer.
    /// </summary>
    public interface IWorkflowImplementationConfiguration
    {
        IPayloadSerializer DefaultPayloadSerializer { get; }
        IActivityInvocationConfiguration DefaultActivityInvocationConfig { get; }
    }

    public class WorkflowImplementationConfiguration : IWorkflowImplementationConfiguration
    {
        public IPayloadSerializer DefaultPayloadSerializer { get; set; }
        public ActivityInvocationConfiguration DefaultActivityInvocationConfig { get; set; }
        IActivityInvocationConfiguration IWorkflowImplementationConfiguration.DefaultActivityInvocationConfig { get { return this.DefaultActivityInvocationConfig; } }
    }

    // -----------

    public interface IActivityInvocationConfiguration
    {
        string TaskQueue { get; }
        int ScheduleToStartTimeoutMillisecs { get; }
        int ScheduleToCloseTimeoutMillisecs { get; }
        int StartToCloseTimeoutMillisecs { get; }
        int HeartbeatTimeoutMillisecs { get; }
        RetryPolicy RetryPolicy { get; }
    }

    public class ActivityInvocationConfiguration : IActivityInvocationConfiguration
    {
        public string TaskQueue { get; set; }
        public int ScheduleToStartTimeoutMillisecs { get; set; }
        public int ScheduleToCloseTimeoutMillisecs { get; set; }
        public int StartToCloseTimeoutMillisecs { get; set; }
        public int HeartbeatTimeoutMillisecs { get; set; }
        public RetryPolicy RetryPolicy { get; set; }
    }

    // ----------- -----------

    public class RetryPolicy
    {
    }

    // ----------- -----------

    /// <summary>
    /// Specifies that a class is an implementation of a workflow that can be hosted by the worker.
    /// 
    /// Can only be applied to public classes. Ifaces, structs and non-public classes are not permitted.
    /// Inheritance is supported.
    /// If multiple Workflow attributes are present within a class due to inheritance, the most derived wins.
    /// 
    /// If 'WorkflowTypeName' is not specified OR null OR Empty OR WhiteSpaceOnly, then the workflow type name is auto-populated
    /// by taking the class type name.
    /// 
    /// 'RunMethod' must be the name of the method that implements them main workflow Run method.
    /// Such method must have one of the following signatures ("RunAsync" is a placeholder for any method name):
    ///     public Task RunAsync();
    ///     public Task RunAsync(IWorkflowContext workflowCtx);
    ///     public Task RunAsync(TArg input) where TArg : IDataValue;
    ///     public Task RunAsync(TArg input, IWorkflowContext workflowCtx) where TArg : IDataValue;
    ///     public Task{TResult} RunAsync() where TResult : IDataValue;
    ///     public Task{TResult} RunAsync(IWorkflowContext workflowCtx) where TResult : IDataValue;
    ///     public Task{TResult} RunAsync(TArg input) where TResult : IDataValue where TArg : IDataValue;
    ///     public Task{TResult} RunAsync(TArg input, IWorkflowContext workflowCtx) where TResult : IDataValue where TArg : IDataValue;
    ///     public Task{PayloadsCollection> RunAsync(PayloadsCollection input, IWorkflowContext workflowCtx);
    /// otherwise an error during worker initialization is generated.
    /// 
    /// If the method named by 'RunMethod' property is overloaded, the property must unambiguously specify a particular overload
    /// by specifying the full signature. In such a case, either ALL or NONE of the types must use the fully qualified type names.
    /// E.g., see <c>Part1_5_AttributesAndInterfaces</c>. There it would be sufficient to specify
    /// 
    /// <code>
    ///     [Workflow(runMethod: "ShopAsync")]
    /// </code>
    /// 
    /// since it is not ambiguous. Other valid options are:
    /// 
    /// <code>
    ///     [Workflow(runMethod: "Task<Part1_5_AttributesAndInterfaces.OrderConfirmation> ShopAsync(Part1_5_AttributesAndInterfaces.User)")]
    ///     [Workflow(runMethod: "System.Threading.Tasks.Task<Temporal.Sdk.BasicSamples.Part1_5_AttributesAndInterfaces.OrderConfirmation> ShopAsync(Temporal.Sdk.BasicSamples.Part1_5_AttributesAndInterfaces.User)")]
    /// </code>
    /// 
    /// The following is will never be matched because it specifies a namespace for Task, but not for the other types:
    /// (note that 'Part1_5_AttributesAndInterfaces' is the outer type, not the namespace, so it must always be present)
    /// 
    /// <code>
    ///     [Workflow(runMethod: "System.Threading.Tasks.Task<Part1_5_AttributesAndInterfaces.OrderConfirmation> ShopAsync(Part1_5_AttributesAndInterfaces.User)")]
    /// </code>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class WorkflowAttribute : Attribute
    {
        public string RunMethod { get; }

        public string WorkflowTypeName { get; set; }

        public bool IsWorkflowTypeNameSetExplicitly { get; }

        public WorkflowAttribute(string runMethod)
        {
            RunMethod = runMethod;
        }

        public override bool IsDefaultAttribute()
        {
            return false;
        }
    }


    /// <summary>
    /// Specifies that a method is an implementation of a signal handler.
    /// Can only be applied to methods defined in classes. When applied inside ifaces or structs, an error will be generated.
    /// Inheritance is supported. However, if a workflow ends up having any ambiguity (e.g. multiple handlers for a particular signal type
    /// or other ambiguities), an error during worker initialization will be generated.
    /// If multiple Workflow attributes are present within a class due to inheritance, the most derived wins.
    /// 
    /// If 'SignalTypeName' is not specified OR null OR Empty OR WhiteSpaceOnly, then the signal type name is auto-populated
    /// by taking the method name and removing 'Async' from its end if present
    /// (if that would result in an empty string, then 'Async' is not removed).
    /// 
    /// Multiple handlers for the same signal type name are prohibited.
    /// 
    /// The method signature must me one of the following:
    ///     public void HandleSignal();
    ///     public void HandleSignal(IWorkflowContext workflowCtx);
    ///     public void HandleSignal(TArg handlerArgs) where TArg : IDataValue;
    ///     public void HandleSignal(TArg handlerArgs, IWorkflowContext workflowCtx) where TArg : IDataValue;
    ///     public Task HandleSignalAsync();
    ///     public Task HandleSignalAsync(IWorkflowContext workflowCtx);
    ///     public Task HandleSignalAsync(TArg handlerArgs) where TArg : IDataValue;
    ///     public Task HandleSignalAsync(TArg handlerArgs, IWorkflowContext workflowCtx) where TArg : IDataValue;
    ///     public Task HandleSignalAsync(PayloadsCollection handlerArgs, IWorkflowContext workflowCtx) where TArg : IDataValue;
    /// otherwise an error during worker initialization is generated.
    /// 
    /// Stub attributes are ignores for all purposes other than validation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class WorkflowSignalHandlerAttribute : Attribute
    {
        public string SignalTypeName { get; set; }
        public WorkflowSignalHandlerAttribute() { }
    }


    /// <summary>
    /// Specifies that a method is an implementation of a query handler.
    /// Can only be applied to methods defined in classes. When applied inside ifaces or structs, an error will be generated.
    /// Inheritance is supported. However, if a workflow ends up having any ambiguity (e.g. multiple handlers for a particular query type
    /// or other ambiguities), an error during worker initialization will be generated.
    /// If multiple Workflow attributes are present within a class due to inheritance, the most derived wins.
    /// 
    /// If 'QueryTypeName' is not specified OR null OR Empty OR WhiteSpaceOnly, then the query type name is auto-populated
    /// by taking the method name and removing 'Async' from its end if present
    /// (if that would result in an empty string, then 'Async' is not removed).
    /// 
    /// Multiple handlers for the same query type name are prohibited.
    /// 
    /// The method signature must me one of the following:
    ///     public TResult HandleQuery();
    ///     public TResult HandleQuery(IWorkflowContext workflowCtx) where TArg : IDataValue where TResult : IDataValue;
    ///     public TResult HandleQuery(TArg queryArgs) where TArg : IDataValue where TResult : IDataValue;
    ///     public TResult HandleQuery(TArg queryArgs, IWorkflowContext workflowCtx) where TArg : IDataValue where TResult : IDataValue;
    ///     public PayloadsCollection HandleQuery(PayloadsCollection queryArgs, IWorkflowContext workflowCtx);
    /// otherwise an error during worker initialization is generated.
    /// 
    /// Stub attributes are ignores for all purposes other than validation.
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public sealed class WorkflowQueryHandlerAttribute : Attribute
    {
        public string QueryTypeName { get; set; }
        public WorkflowQueryHandlerAttribute() { }
    }

    // ----------- -----------
}
