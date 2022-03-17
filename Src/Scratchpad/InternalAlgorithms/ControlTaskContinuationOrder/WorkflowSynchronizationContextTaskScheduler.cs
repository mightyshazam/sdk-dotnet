using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace ControlTaskContinuationOrder
{
    /// <summary>
    /// A TaskScheduler implementation that executes all tasks queued to it through a call to
    /// <see cref="System.Threading.SynchronizationContext.Post"/> on the <see cref="System.Threading.SynchronizationContext"/>
    /// that its associated with.
    /// The default constructor for this class binds to the current <see cref="System.Threading.SynchronizationContext"/>
    /// The implementation is copied from the .NET sources and modified to prevent in-line task execution, forcing in-line execution
    /// requests to run asynchronously.
    /// </summary>
    internal class WorkflowSynchronizationContextTaskScheduler : TaskScheduler
    {
        public const bool PermitInlineExecution = false;
        
        private readonly WorkflowSynchronizationContext _syncCtx;
        private readonly WaitCallback _executeTaskDelegate;
        private readonly Queue<Task> _scheduledTasks = new Queue<Task>();
        private readonly object _executeScheduledTasksLoopLock = new object();

        private TaskCompletionSource<bool> _tasksScheduledCompletion = null;

        public WorkflowSynchronizationContextTaskScheduler(WorkflowSynchronizationContext syncCtx)
            : base()
        {
            if (syncCtx == null)
            {
                throw new ArgumentNullException(nameof(syncCtx));
            }

            _syncCtx = syncCtx;
            _executeTaskDelegate = ExecuteTask;
        }

        /// <summary>
        /// Implements the <see cref="System.Threading.Tasks.TaskScheduler.MaximumConcurrencyLevel"/> property this scheduler.
        /// Returns 1, because a <see cref="WorkflowSynchronizationContext"/> runs items sequentially.        
        /// </summary>
        public override int MaximumConcurrencyLevel
        {
            get { return 1; }
        }

        public int ScheduledTasksCount
        {
            get { lock (_scheduledTasks) { return _scheduledTasks.Count; } }
        }

        public override string ToString()
        {
            return nameof(WorkflowSynchronizationContextTaskScheduler) + $"(Id={Id}, SyncCtx.Id={_syncCtx.Id})";
        }

        /// <summary>
        /// Implementation of <see cref="System.Threading.Tasks.TaskScheduler.QueueTask"/> for this scheduler class.        
        /// Simply posts the tasks to be executed on the associated <see cref="System.Threading.SynchronizationContext"/>.
        /// </summary>
        protected override void QueueTask(Task task)
        {
            Program.WriteLine($"**{this.ToString()}.QueueTask(task.Id={task?.Id}): 1");

            if (task != null)
            {
                EqueueScheduledTask(task);
            }

            Program.WriteLine($"**{this.ToString()}.QueueTask(task.Id={task?.Id}): End");
        }

        /// <summary>
        /// Implementation of <see cref="System.Threading.Tasks.TaskScheduler.TryExecuteTaskInline"/> for this scheduler class.
        /// The task will be executed in-line only the compile time setting <see cref="PermitInlineExecution" /> is
        /// set to <c>true</c> AND if the call happens within the associated <see cref="SynchronizationContext" />.
        /// </summary>        
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            Program.WriteLine($"**!!!{this.ToString()}.TryExecuteTaskInline(task.Id={task?.Id}, previouslyQueued={taskWasPreviouslyQueued}).");

            if (PermitInlineExecution && !taskWasPreviouslyQueued && (SynchronizationContext.Current == _syncCtx))
            {
                return TryExecuteTask(task);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// This method overrides <see cref="TaskScheduler.GetScheduledTasks" />. As described for any such override,
        /// this API is intended for integration with debuggers. It will only be invoked when a debugger requests the
        /// data. The returned tasks will be used by debugging tools to access the currently queued tasks, in order to
        /// provide a representation of this information in the UI.
        /// It is important to note that, when this method is called, all other threads in the process will
        /// be frozen. Therefore, it's important to avoid synchronization. Thus, we do not lock on <c>_scheduledTasks</c>
        /// and instead catch and gracefully give up in case of concurrent access errors.
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            List<Task> scheduledTasks = new(capacity: _scheduledTasks.Count);

            try
            {
                foreach (Task task in _scheduledTasks)
                {
                    scheduledTasks.Add(task);
                }
            }
            catch
            {
                // If we could not access all _scheduledTasks, just return what we have.
            }

            return scheduledTasks;
        }

        public Task WhenTasksScheduledAsync()
        {
            lock(_scheduledTasks)
            {
                _tasksScheduledCompletion = _tasksScheduledCompletion ?? new TaskCompletionSource<bool>();
                if (_scheduledTasks.Count > 0)
                {
                    _tasksScheduledCompletion.TrySetResult(true);
                }
                       
                return _tasksScheduledCompletion.Task;                
            }
        }

        public void ExecuteScheduledTasks(bool breakOnYield)
        {
            Program.WriteLine($"**{this.ToString()}.ExecuteScheduledTasks(breakOnYield={breakOnYield}): 1  (scheduledCount={_scheduledTasks.Count})");

            using ThreadPoolInvocationState invocationState = new();
            object errorInfo = null;

            // Generally, this method could be invoked concurrently from different threads, but that may break the
            // strict ordering guarantee this scheduler provides. By synchronizing on the loop we avoid that.
            lock (_executeScheduledTasksLoopLock)
            {
                while (TryDequeueScheduledTask(out Task scheduledTask))
                {
                    Program.WriteLine($"**{this.ToString()}.ExecuteScheduledTasks(breakOnYield={breakOnYield}): 2  (scheduledCount={_scheduledTasks.Count}, taskId={scheduledTask.Id})");

                    invocationState.Reset(scheduledTask);

                    ThreadPool.QueueUserWorkItem(_executeTaskDelegate, invocationState);

                    invocationState.WaitForCompletion();
                    if (invocationState.TryGetException(out Exception ex))
                    {
                        Program.WriteLine($"**{this.ToString()}.ExecuteScheduledTasks(): 3  (ex='{ex.GetType()}: {ex.Message}', taskId={scheduledTask.Id})");

                        if (errorInfo == null)
                        {
                            errorInfo = ex;
                        }
                        else if (errorInfo is List<Exception> errorList)
                        {
                            errorList.Add(ex);
                        }
                        else
                        {
                            errorList = new List<Exception>();
                            errorList.Add((Exception) errorInfo);
                            errorList.Add(ex);
                        }
                    }

                    Program.WriteLine($"**{this.ToString()}.ExecuteScheduledTasks(): 4  (scheduledCount={_scheduledTasks.Count}, taskId={scheduledTask.Id})"
                                     + "\n *********** *********** *********** *********** *********** *********** ***********\n");

                    if (breakOnYield
                            && TryPeekScheduledTask(out Task lookaheadTask)
                            && IsWorkActionInfoSet(lookaheadTask, WorkflowSynchronizationContext.WorkAction.Metadata.IsYieldContinuation))
                    {
                        Program.WriteLine($"**{this.ToString()}.ExecuteScheduledTasks(): 5 (breakOnYield) (scheduledCount={_scheduledTasks.Count}, taskId={scheduledTask.Id})");
                        break;
                    }
                }
            }

            if (errorInfo != null)
            {
                if (errorInfo is Exception exception)
                {
                    ExceptionDispatchInfo.Capture(exception).Throw();
                }
                else
                {
                    throw new AggregateException((List<Exception>) errorInfo);
                }
            }

            Program.WriteLine($"**{this.ToString()}.ExecuteScheduledTasks(): End"
                            + "\n ::::::::::: ::::::::::: ::::::::::: ::::::::::: ::::::::::: ::::::::::: :::::::::::\n");
        }

        private bool TryPeekScheduledTask(out Task task)
        {
            lock (_scheduledTasks)
            {
                if (_scheduledTasks.Count > 0)
                {
                    task = _scheduledTasks.Peek();
                    return true;
                }

                task = null;
                return false;
            }
        }

        private bool TryDequeueScheduledTask(out Task task)
        {
            lock (_scheduledTasks)
            {
                if (_scheduledTasks.Count > 0)
                { 
                    task = _scheduledTasks.Dequeue();

                    if (_scheduledTasks.Count == 0)
                    {
                        Debug.Assert(_tasksScheduledCompletion == null || _tasksScheduledCompletion.Task.IsCompleted,
                                     "We dequeued a Task, so _tasksScheduledCompletion must be either NULL or in Completed state.");
                        _tasksScheduledCompletion = null;
                    }

                    return true;
                }

                task = null;
                return false;
            }
        }

        private void EqueueScheduledTask(Task task)
        {
            lock (_scheduledTasks)
            {
                _scheduledTasks.Enqueue(task);
                _tasksScheduledCompletion?.TrySetResult(true);
            }
        }

        private void ExecuteTask(object invocationStateObject)
        {
            if (invocationStateObject == null)
            {
                throw new ArgumentNullException(nameof(invocationStateObject));
            }

            if (! (invocationStateObject is ThreadPoolInvocationState invocationState))
            {
                throw new ArgumentException($"The specified {nameof(invocationStateObject)} was expected to be of type"
                                          + $" {nameof(ThreadPoolInvocationState)}, but the actual type"
                                          + $" was {invocationStateObject.GetType().FullName}.");
            }

            try
            {
                if (TryExecuteTaskInContext(invocationState.Task))
                {
                    invocationState.TrySetStatusSucceeded();
                }
                else
                {
                    invocationState.TrySetStatusCannotExecute();
                }
            }
            catch (Exception ex)
            {
                invocationState.TrySetStatusFailed(ex);
            }
        }

        private bool TryExecuteTaskInContext(Task task)
        {
            Program.WriteLine($"**{this.ToString()}.TryExecuteTaskInContext(task.Id={task.Id}): 1");

            SynchronizationContext prevSyncCtx = SynchronizationContext.Current;

            bool canSelfInstallSyncCtx = IsWorkActionInfoSet(task, WorkflowSynchronizationContext.WorkAction.Metadata.CanInstallSyncCtxInternally);
            bool installSyncCtx = (prevSyncCtx != _syncCtx) && !canSelfInstallSyncCtx;

            if (installSyncCtx)
            {
                SynchronizationContext.SetSynchronizationContext(_syncCtx);
            }

            bool canExecute;
            try
            {
                Program.WriteLine($"**{this.ToString()}.TryExecuteTaskInContext(task.Id={task.Id}): 2  (instlCtx={installSyncCtx}, canSelfInstl={canSelfInstallSyncCtx})");
                canExecute = TryExecuteTask(task);
                Program.WriteLine($"**{this.ToString()}.TryExecuteTaskInContext(task.Id={task.Id}): 3  (instlCtx={installSyncCtx}, canSelfInstl={canSelfInstallSyncCtx})");
            }
            finally
            {
                if (installSyncCtx)
                {
                    SynchronizationContext.SetSynchronizationContext(prevSyncCtx);
                }
            }

            Program.WriteLine($"**{this.ToString()}.TryExecuteTaskInContext(task.Id={task.Id}): End  (canExecute={canExecute})");
            return canExecute;
        }

        private static bool IsWorkActionInfoSet(Task task, WorkflowSynchronizationContext.WorkAction.Metadata check)
        {
            if (TryGetEmbeddedWorkAction(task, out WorkflowSynchronizationContext.WorkAction asyncCtxAction))
            {
                return asyncCtxAction.IsInfoSet(check);
            }

            return false;
        }

        private static bool TryGetEmbeddedWorkAction(Task task, out WorkflowSynchronizationContext.WorkAction asyncCtxWorkAction)
        {
            if (task != null && task.AsyncState != null && task.AsyncState is WorkflowSynchronizationContext.WorkAction asyncCtxAction)
            {
                asyncCtxWorkAction = asyncCtxAction;
                return true;
            }

            asyncCtxWorkAction = null;
            return false;
        }

        private sealed class ThreadPoolInvocationState : IDisposable
        {
            public static class ExecutionStatus
            {
                public const int NotStarted = 0;
                public const int CannotExecute = 2;
                public const int Succeeded = 3;
                public const int Failed = 4;
                
            }

            private Task _task = null;
            private Exception _exception = null;
            private int _status = ExecutionStatus.NotStarted;
            private ManualResetEventSlim _completionSignal = new(initialState: false);

            public Task Task
            {
                get { return _task; }
            }

            public int Status
            {
                get { return _status; }
            }

            private ManualResetEventSlim GetCompletionSignal()
            {
                ManualResetEventSlim completionSignal = _completionSignal;
                if (completionSignal == null)
                {
                    throw new ObjectDisposedException($"This {nameof(ThreadPoolInvocationState)} instance is already disposed.");
                }

                return completionSignal;
            }

            public bool TrySetStatusCannotExecute()
            {
                if (ExecutionStatus.NotStarted != Interlocked.CompareExchange(ref _status, ExecutionStatus.CannotExecute, ExecutionStatus.NotStarted))
                {
                    return false;
                }

                GetCompletionSignal().Set();
                return true;
            }

            public bool TrySetStatusSucceeded()
            {
                if (ExecutionStatus.NotStarted != Interlocked.CompareExchange(ref _status, ExecutionStatus.Succeeded, ExecutionStatus.NotStarted))
                {
                    return false;
                }

                GetCompletionSignal().Set();
                return true;
            }

            public bool TrySetStatusFailed(Exception exception)
            {
                if (ExecutionStatus.NotStarted != Interlocked.CompareExchange(ref _status, ExecutionStatus.Failed, ExecutionStatus.NotStarted))
                {
                    return false;
                }

                _exception = exception;
                GetCompletionSignal().Set();
                return true;
            }

            public void WaitForCompletion()
            {
                GetCompletionSignal().Wait();
            }

            public bool TryGetException(out Exception exception)
            {
                exception = _exception;
                return (exception != null);
            }

            public void Reset(Task task)
            {
                if (task == null)
                {
                    throw new ArgumentNullException(nameof(task));
                }

                ResetCore(task);
            }

            public void Dispose()
            {
                ResetCore(task: null);
                ManualResetEventSlim completionSignal = Interlocked.Exchange(ref _completionSignal, null);
                completionSignal?.Dispose();
            }

            private void ResetCore(Task task)
            {
                _task = task;
                _exception = null;
                _status = ExecutionStatus.NotStarted;
                GetCompletionSignal().Reset();
            }
        }  // class ThreadPoolInvocationState
    }
}
