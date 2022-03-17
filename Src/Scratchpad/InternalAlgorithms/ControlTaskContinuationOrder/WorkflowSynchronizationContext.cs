using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ControlTaskContinuationOrder
{
    public sealed class WorkflowSynchronizationContext : SynchronizationContext
    {
        #region Static Id management

        private static int s_lastId = 0;

        private static int GetNextId()
        {
            int id = Interlocked.Increment(ref s_lastId);
            while (id > (Int32.MaxValue / 2))
            {
                Interlocked.Exchange(ref s_lastId, 0);
                id = Interlocked.Increment(ref s_lastId);
            }

            return id;
        }

        #endregion Static Id management

        public const TaskCreationOptions DefaultTaskCreationOptions = TaskCreationOptions.PreferFairness
                                                                    | TaskCreationOptions.DenyChildAttach
                                                                    | TaskCreationOptions.RunContinuationsAsynchronously;

        public const TaskContinuationOptions DefaultTaskContinuationOptions = TaskContinuationOptions.PreferFairness
                                                                            | TaskContinuationOptions.DenyChildAttach
                                                                            | TaskContinuationOptions.LazyCancellation;

        internal record WorkAction(SendOrPostCallback Action, object UserState, WorkAction.Metadata Info)
        {
            [Flags]
            public enum Metadata
            {
                None = 0,
                CanInstallSyncCtxInternally = 2,
                IsYieldContinuation = 4,
            }

            public bool IsInfoSet(WorkAction.Metadata check)
            {
                return (check == (Info & check)) ;
            }
        }

        private readonly WorkflowSynchronizationContextTaskScheduler _taskScheduler;
        private readonly TaskFactory _postedCallbacksTaskFactory;
        private readonly Action<object> _executeWorkActionInContextDelegate;

        public WorkflowSynchronizationContext()                        
            : this(originalId: null)
        {            
        }

        private WorkflowSynchronizationContext(int? originalId)
        {
            Id = GetNextId();
            OriginalId = originalId ?? Id;
            _executeWorkActionInContextDelegate = ExecuteWorkActionInContext;
            _taskScheduler = new WorkflowSynchronizationContextTaskScheduler(this);
            _postedCallbacksTaskFactory = CreateNewTaskFactory(CancellationToken.None);
        }

        public int Id { get; }

        public int OriginalId { get; }

        public TaskScheduler TaskScheduler
        {
            get { return _taskScheduler; }
        }

        public override string ToString()
        {
            return $"{nameof(WorkflowSynchronizationContext)}(Id={Id}, OriginalId={OriginalId})";
        }

        public TaskFactory CreateNewTaskFactory(CancellationToken cancelToken)
        {
            return new TaskFactory(cancelToken, DefaultTaskCreationOptions, DefaultTaskContinuationOptions, TaskScheduler);
        }

        public TaskFactory<TResult> CreateNewTaskFactory<TResult>(CancellationToken cancelToken)
        {
            return new TaskFactory<TResult>(cancelToken, DefaultTaskCreationOptions, DefaultTaskContinuationOptions, TaskScheduler);
        }

        public override SynchronizationContext CreateCopy()
        {
            return new WorkflowSynchronizationContext(OriginalId);
        }

        /// <summary>Dispatches a synchronous message. NOT Supported - will throw <c>NotSupportedException</c>!</summary>        
        public override void Send(SendOrPostCallback workAction, object state)
        {
            throw new NotSupportedException($"{nameof(WorkflowSynchronizationContext)}.{nameof(Send)}(..) is not supported.");
        }

        /// <summary>Dispatches an asynchronous message.</summary>   
        public override void Post(SendOrPostCallback workAction, object state)
        {
            Program.WriteLine($"**{this.ToString()}.Post(..): 1");

            if (workAction == null)
            {
                Program.WriteLine($"**{this.ToString()}.Post(..): 2  (GiveUp)");
                return;
            }

            WorkAction.Metadata waInfo = WorkAction.Metadata.CanInstallSyncCtxInternally;

            if (IsYieldContinuation(workAction, state))
            {
                waInfo |= WorkAction.Metadata.IsYieldContinuation;
            }
                        
            _postedCallbacksTaskFactory.StartNew(_executeWorkActionInContextDelegate, new WorkAction(workAction, state, waInfo));

            Program.WriteLine($"**{this.ToString()}.Post(..): End  (IsYield={0 != (waInfo & WorkAction.Metadata.IsYieldContinuation)})");
        }

        /// <summary>
        /// We want to highlight continuations posted to Post(..) by Task.Yield().
        /// That will allow the ExecutePostedWorkActions(..) loop to break if such a yield marker is encountered.
        /// !!! Attention !!!
        /// This check for the yield continuation relies on an implementation detail of the .NET Framework.
        /// It must be explicitly tested on all supported .NET versions. The check logic is:
        ///  * The specified 'workAction' points to the static method 'RunAction(..)' declared
        ///    by type 'System.Runtime.CompilerServices.YieldAwaitable+YieldAwaiter'.
        ///  * The specified state is a non-null instance of type 'System.Action'
        ///    (we relax the verification to accept any Delegate).
        /// </summary>        
        private static bool IsYieldContinuation(SendOrPostCallback workAction, object state)
        {
            if (workAction == null || state == null)
            {
                return false;
            }

            MethodInfo workActionMethod = workAction.Method;
            return (workActionMethod != null
                        && state is Delegate
                        && workActionMethod.DeclaringType != null
                        && workActionMethod.DeclaringType == typeof(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter)
                        && workActionMethod.IsStatic == true
                        && workActionMethod.Name != null
                        && workActionMethod.Name.Equals("RunAction", StringComparison.Ordinal));
        }

        public void ExecutePostedWorkActions(bool breakOnYield)
        {
            Program.WriteLine($"**{this.ToString()}.ExecutePostedWorkActions(breakOnYield={breakOnYield}): 1  (SchdldTsksCnt={_taskScheduler.ScheduledTasksCount})");

            _taskScheduler.ExecuteScheduledTasks(breakOnYield);
            
            Program.WriteLine($"**{this.ToString()}.InvokeAllPostedWorkActions(breakOnYield={breakOnYield}): End");
        }
        public Task WhenWorkActionsScheduledAsync()
        {
            return _taskScheduler.WhenTasksScheduledAsync();
        }

        private void ExecuteWorkActionInContext(object workObject)
        {
            Program.WriteLine($"**{this.ToString()}.ExecuteWorkActionInContext(): 1");

            WorkAction work = (WorkAction) workObject;

            SynchronizationContext prevSyncCtx = SynchronizationContext.Current;

            bool installSyncCtx = (prevSyncCtx != this);
            if (installSyncCtx)
            {
                SynchronizationContext.SetSynchronizationContext(this);
            }

            try
            {
                Program.WriteLine($"**{this.ToString()}.ExecuteWorkActionInContext(): 2  (installSyncCtx={installSyncCtx})");
                work.Action(work.UserState);
                Program.WriteLine($"**{this.ToString()}.ExecuteWorkActionInContext(): 3  (installSyncCtx={installSyncCtx})");
            }
            finally
            {
                if (installSyncCtx)
                {
                    SynchronizationContext.SetSynchronizationContext(prevSyncCtx);
                }
            }

            Program.WriteLine($"**{this.ToString()}.ExecuteWorkActionInContext(): End");
        }
    }
}
