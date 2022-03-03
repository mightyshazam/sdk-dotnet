using System;
using System.Threading.Tasks;

using Temporal.Common.DataModel;
using Temporal.Worker.Workflows.Base;

namespace Temporal.Worker.Workflows.Dynamic
{
    public class DynamicWorkflowsApi
    {
    }

    // =========== Dynamic workflow abstractions ===========
    // When users want to dynamically (at runtime) map implementations to signals / queries,
    // they will use these abstractions.

    public abstract class DynamicWorkflowBase<TInput, TResult> : BasicWorkflowBase
                where TInput : IDataValue
                where TResult : IDataValue
    {
        public sealed override async Task<PayloadsCollection> RunAsync(IWorkflowContext workflowCtx)
        {
            // Check the data converter logic here....

            TInput input = (typeof(TInput) == typeof(IDataValue.Void))
                    ? (TInput) (IDataValue) IDataValue.Void.Instance
                    : workflowCtx.GetDataConverter().Deserialize<TInput>(workflowCtx.CurrentRun.Input);

            IDynamicWorkflowContext dynamicCtx = null;  // some wrapper of workflowCtx;

            TResult result = await RunAsync(input, dynamicCtx);

            PayloadsCollection serializedResult = (result == null || result.GetType() == typeof(IDataValue.Void))
                    ? PayloadsCollection.Empty
                    : workflowCtx.WorkflowImplementationConfig.DefaultDataConverter.Serialize<TResult>(result);

            return serializedResult;
        }

        public abstract Task<TResult> RunAsync(TInput input, IDynamicWorkflowContext workflowCtx);

        public sealed override PayloadsCollection HandleQuery(string queryName, PayloadsCollection input, IWorkflowContext workflowCtx)
        {
            // Handle query as specified by the dynamic workflow APIs. If none of the configured handlers or the default pocily applies,
            // fall back to the base implementation.
            return base.HandleQuery(queryName, input, workflowCtx);            
        }

        public sealed override Task HandleSignalAsync(string signalName, PayloadsCollection input, IWorkflowContext workflowCtx)
        {
            // Handle signal as specified by the dynamic workflow APIs. If none of the configured handlers or the default pocily applies,
            // fall back to the base implementation.
            return base.HandleSignalAsync(signalName, input, workflowCtx);
        }
    }

    public abstract class DynamicWorkflowBase : DynamicWorkflowBase<IDataValue.Void, IDataValue.Void>    
    {
        public sealed override async Task<IDataValue.Void> RunAsync(IDataValue.Void _, IDynamicWorkflowContext workflowCtx)
        {
            await RunAsync(workflowCtx);
            return null;
        }

        public abstract Task RunAsync(IDynamicWorkflowContext workflowCtx);
    }


    public abstract class DynamicWorkflowBase<TResult>: DynamicWorkflowBase<IDataValue.Void, TResult>
                where TResult : IDataValue
    {
        public sealed override async Task<TResult> RunAsync(IDataValue.Void _, IDynamicWorkflowContext workflowCtx)
        {
            TResult result = await RunAsync(workflowCtx); // no await required, but using for consistency with non-generic DynamicWorkflowBase.
            return result;
        }
        
        public abstract Task<TResult> RunAsync(IDynamicWorkflowContext workflowCtx);
    }

    // ---

    public interface IDynamicWorkflowContext : IWorkflowContext
    {        
        IDynamicWorkflowController DynamicControl { get; }
    }

    public static class WorkflowContextExtensions
    {
        public static bool CanCastAsDynamic(this IWorkflowContext workflowCtx) { return false; }
        public static bool TryCastAsDynamic(this IWorkflowContext workflowCtx, out IDynamicWorkflowContext dynamicWorkflowCtx) { dynamicWorkflowCtx = null; return false; }
        public static IDynamicWorkflowContext CastAsDynamic(this IWorkflowContext workflowCtx) { return null; }
    }

    /// <summary>SignalHandlers are specified as a triple (priority, matcherRegex, handlerDelegate).
    /// (Queries are handled in equivalent manner, including default policy.)
    /// When a signal with a name 'SignalName' is received, we evaluate all respective triples in the order of priority, until
    /// SignalName is completely matched by the respective matcherRegex. Then the handlerDelegate is invoked to process the 
    /// signal.
    /// If no matcherRegex matches the received SignalName, we evaluate whether the current DefaultSignalHandlerPolicy applies.
    /// Such policy usually comes with its own matcherRegex and applies to all signals that are not matched by any handlers,
    /// but are matched by the policy's matcherRegex. It may specify catch-all behaviors such as "cache the signal an process
    /// it if and when a matching signal handler is configures", "ignore the signal" or other
    /// (<see cref="DefaultSignalHandlerPolicy"/>).
    /// If SignalName is not matched by the default policy's matcherRegex, the policy does not apply.
    /// In that case, the signal will fall through to the hard-coded default provided by
    /// <see cref="BasicWorkflowBase.HandleSignalAsync(String, PayloadsCollection, WorkflowContext)"/>.
    /// This will likely be ignoring the signal logging an error.</summary>
    public interface IDynamicWorkflowController
    {
        IHandlerCollection<Func<string, IDataValue, IWorkflowContext, Task>> SignalHandlers { get; }
        IHandlerCollection<Func<string, IDataValue, IWorkflowContext, IDataValue>> QueryHandlers { get; }

        SignalHandlingOrderPolicy SignalHandlingOrderPolicy { get; set; }

        SignalHandlerDefaultPolicy SignalHandlerDefaultPolicy { get; set; }
        QueryHandlerDefaultPolicy QueryHandlerDefaultPolicy { get; set; }
    }

    public interface IHandlerCollection<THandler> where THandler : class
    {
        int Count { get; }
        void Clear();
        void GetAt(int index, out string matcherRegex, out THandler handler);
        void RemoveAt(int index);
        void RemoveAt(int index, out string matcherRegex, out THandler handler);
        bool TryAdd(string matcherRegex, THandler handler);
        bool TryInsert(int index, string matcherRegex, THandler handler);
        bool TryUpdateAt(int index, string matcherRegex, THandler handler);
        bool TryGetByRegexPattern(string matcherRegex, out int index);
        bool TryGetByRegexPattern(string matcherRegex, out int index, out THandler handler);
        bool TryFindFirstMatch(string itemToMatch, out int index);
        bool TryFindFirstMatch(string itemToMatch, out int index, out string matcherRegex, out THandler handler);        
    }

    /// <summary>This setting affects the processing order of signals. It is independent of SignalHandlerDefaultPolicy.
    /// However, unless <see cref="SignalHandlerDefaultPolicy" /> is caching, any of these settings result in the same
    /// semantic behavior. Should this instead be a parameter to
    /// <see cref="SignalHandlerDefaultPolicy.CacheAndProcessWhenHandlerIsSet(String)" />?
    /// <br />
    /// Need to design when a a cached signal is handled if a matching handler is added:
    ///  - immediately?
    ///  - workflow should call some kind of HandleNewlyEnabledSingals() API?
    ///  - at the end of the current workflow task (at the start there was no handler yet)?
    ///  - at the satart of next workflow task, before additional signals that arrive in the meantime?
    ///  - something else?
    /// This choice may simplify some of the <c>SignalHandlingOrderPolicy</c>-options.</summary>
    public class SignalHandlingOrderPolicy
    {
        /// <summary>Handle signal as soon as it arrives, *if* a matching handler is configured.
        /// If a handler is added for cached signals, handle them immediately.</summary>
        public static SignalHandlingOrderPolicy AsSoonAsPossible() { return null; }

        /// <summary>Handle signals strictly in the order of arrival. A cached signal is considered no-yet-handled.
        /// Thus, if there any signals in the cache, all arriving signals are cached, even if a matching handlers exists.
        /// If a matching handler is added for a cached signal, but there are earlier signals that have no matching handlers,
        /// such signal will not be handled until all earlier signals are handled.
        /// Adding a matching handler for the earliest not-yet-handled signal can trigger several signals queueued up in this
        /// manner to be handled immadiately.</summary>
        public static SignalHandlingOrderPolicy Strict() { return null; }

        /// <summary>Handle signal as soon as it arrives, *if* a matching handler is configured.
        /// If a handler is added for a cached signals, handle them immediately in first-in-last-out order.
        /// No strict ordering, i.e. if a cached signal does not have a matching handler, it will not affect
        /// other cached signals, regardless of whether they arrived before or after.</summary>
        public static SignalHandlingOrderPolicy OnArrivalOrReversedFromCache() { return null; }

        /// <summary>Handle signal as soon as it arrives, *if* a matching handler is configured.
        /// If a handler is added for cached signals, handle them strictly in first-in-first-out order.
        /// I.e., there is strict ordering (similar to <see cref="SignalHandlingOrderPolicy.Strict" />) only
        /// for signals that are readily in the cache.</summary>
        public static SignalHandlingOrderPolicy OnArrivalOrStrictFromCache() { return null; }

        /// <summary>Handle signal as soon as it arrives, *if* a matching handler is configured.
        /// If a handler is added for cached signals, handle them strictly in first-in-last-out order.
        /// I.e., there is strict reversed ordering (similar to <see cref="SignalHandlingOrderPolicy.Strict" />) only
        /// for signals that are readily in the cache.</summary>
        public static SignalHandlingOrderPolicy OnArrivalOrStrictReversedFromCache() { return null; }
    }

    public class SignalHandlerDefaultPolicy
    {
        public static SignalHandlerDefaultPolicy CacheAndProcessWhenHandlerIsSet(string matcherRegex) { return null; }
        public static SignalHandlerDefaultPolicy CustomHandler(string matcherRegex, Func<string, IDataValue, IWorkflowContext, Task> handler) { return null; }
        public static SignalHandlerDefaultPolicy Ignore(string matcherRegex) { return null; }
        public static SignalHandlerDefaultPolicy None() { return null; }

        public string Name { get; }
        public string MatcherRegex { get; }
        public bool IsMatch(string signalName) { return false; }

        public bool TryClearCache() { return false; }
    }

    public class QueryHandlerDefaultPolicy
    {
        public static QueryHandlerDefaultPolicy ConstantResultValue(string matcherRegex, IDataValue resultValue) { return null; }
        public static QueryHandlerDefaultPolicy CustomHandler(string matcherRegex, Func<string, IDataValue, IWorkflowContext, IDataValue> handler) { return null; }
        public static QueryHandlerDefaultPolicy CustomError(string matcherRegex, Func<string, IDataValue, Exception> errorFactory) { return null; }
        public static QueryHandlerDefaultPolicy None() { return null; }

        public string Name { get; }
        public string MatcherRegex { get; }
        public bool IsMatch(string queryName) { return false; }
    }

    public static class SignalHandler
    {
        public static Func<string, IDataValue, IWorkflowContext, Task> Create(Action handler)
        {
            return (_, _, _) => Adapter(handler);
        }

        public static Func<string, IDataValue, IWorkflowContext, Task> Create(Action<string> handler)
        {
            return (signalName, _, _) => Adapter(handler, signalName);
        }

        public static Func<string, IDataValue, IWorkflowContext, Task> Create<TInput>(Action<string, TInput> handler) where TInput : IDataValue
        {
            return null;
        }

        public static Func<string, IDataValue, IWorkflowContext, Task> Create(Func<Task> handler)
        {
            return (_, _, _) => Adapter(handler);
        }

        public static Func<string, IDataValue, IWorkflowContext, Task> Create(Func<string, Task> handler)
        {
            return (signalName, _, _) => Adapter(handler, signalName);
        }

        public static Func<string, IDataValue, IWorkflowContext, Task> Create<TInput>(Func<string, TInput, Task> handler) where TInput : IDataValue
        {
            return null;
        }

        public static Func<string, IDataValue, IWorkflowContext, Task> Create<TInput>(Func<string, TInput, IWorkflowContext, Task> handler) where TInput : IDataValue
        {
            return null;
        }

        private static Task Adapter(Action handler)
        {
            handler();
            return Task.CompletedTask;            
        }

        private static Task Adapter(Action<string> handler, string signalName)
        {
            handler(signalName);
            return Task.CompletedTask;            
        }

        private static Task Adapter(Func<Task> handler)
        {
            return handler();            
        }

        private static Task Adapter(Func<string, Task> handler, string signalName)
        {
            return handler(signalName);
        }
    }

    public static class QueryHandler
    {
        public static Func<string, IDataValue, IWorkflowContext, IDataValue> Create<TResult>(Func<TResult> handler) where TResult : IDataValue
        {
            return (_, _, _) => Adapter(handler);
        }

        public static Func<string, IDataValue, IWorkflowContext, IDataValue> Create<TResult>(Func<string, TResult> handler) where TResult : IDataValue
        {
            return (queryName, _, _) => Adapter(handler, queryName);
        }

        public static Func<string, IDataValue, IWorkflowContext, IDataValue> Create<TInput, TResult>(Func<string, TInput, TResult> handler)
                    where TInput : IDataValue
                    where TResult : IDataValue
        {
            return null;
        }

        public static Func<string, IDataValue, IWorkflowContext, IDataValue> Create<TInput, TResult>(Func<string, TInput, IWorkflowContext, TResult> handler)
                    where TInput : IDataValue
                    where TResult : IDataValue
        {
            return null;
        }

        private static TResult Adapter<TResult>(Func<TResult> handler) where TResult : IDataValue
        {
            return handler();        
        }

        private static TResult Adapter<TResult>(Func<string, TResult> handler, string queryName) where TResult : IDataValue
        {
            return handler(queryName);
        }
    }
}
