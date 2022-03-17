using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

using TaskType = System.Threading.Tasks.Task;

namespace ControlTaskContinuationOrder
{
    public static class WorkflowRoutine
    {
        public struct Void
        {
            public static readonly Void Instance = default(Void);
            public static readonly Task<Void> CompletedTask = Task.FromResult(Instance);

            public override string ToString()
            {
                return nameof(Void);
            }
        }

        private record TaskExecutionContext<TArg, TResult>(Func<TArg, CancellationToken, Task<TResult>> RoutineFunc,
                                                           TArg UserState,
                                                           CancellationToken CancelToken);

        private static class RoutineTaskExecutorFuncCache<TArg, TResult>
        {
            private static readonly Func<object, Task<TResult>> s_func = Execute<TArg, TResult>;

            internal static Func<object, Task<TResult>> Func
            {
                get { return s_func; }
            }

        }

        private static void ValidateRoutineTaskNotNull(Task routineTask, string routineFuncParamName)
        {
            if (routineTask == null)
            {
                throw new InvalidOperationException($"The {routineFuncParamName} specified for this {nameof(WorkflowRoutine)}"
                                                  + $" returned a null Task. A {routineFuncParamName} specified for a"
                                                  + $" {nameof(WorkflowRoutine)} must return a valid Task instance.");
            }
        }

        private static async Task<Void> ExecuteAndReturnVoidResult(Func<Task> routineFunc)
        {
            Task routineTask = routineFunc();

            ValidateRoutineTaskNotNull(routineTask, nameof(routineFunc));
            await routineTask;

            return Void.Instance;
        }

        private static async Task<Void> ExecuteAndReturnVoidResult<TArg>(Func<TArg, Task> routineFunc, TArg state)
        {
            Task routineTask = routineFunc(state);

            ValidateRoutineTaskNotNull(routineTask, nameof(routineFunc));
            await routineTask;

            return Void.Instance;
        }

        private static async Task<Void> ExecuteAndReturnVoidResult<TArg>(Func<TArg, CancellationToken, Task> routineFunc, TArg state, CancellationToken cancelToken)
        {
            Task routineTask = routineFunc(state, cancelToken);

            ValidateRoutineTaskNotNull(routineTask, nameof(routineFunc));
            await routineTask;

            return Void.Instance;
        }

        public static WorkflowRoutine<Void> Start(Action routineFunc)
        {
            return Start<Void, Void>( (_, _) => { routineFunc(); return Void.CompletedTask; }, Void.Instance, CancellationToken.None);
        }

        public static WorkflowRoutine<Void> Start(Func<Task> routineFunc)
        {
            return Start<Void, Void>( (_, _) => ExecuteAndReturnVoidResult(routineFunc), Void.Instance, CancellationToken.None);
        }

        public static WorkflowRoutine<Void> Start<TArg>(Func<TArg, Task> routineFunc, TArg state)
        {
            return Start<TArg, Void>( (s, _) => ExecuteAndReturnVoidResult(routineFunc, s), state, CancellationToken.None);
        }

        public static WorkflowRoutine<TResult> Start<TResult>(Func<Task<TResult>> routineFunc)
        {
            return Start<Void, TResult>( (_, _) => routineFunc(), Void.Instance, CancellationToken.None);
        }

        public static WorkflowRoutine<TResult> Start<TArg, TResult>(Func<TArg, CancellationToken, Task<TResult>> routineFunc,
                                                                    TArg state,
                                                                    CancellationToken cancelToken)
        {
            WorkflowSynchronizationContext routineSyncCtx = new();
            Task<TResult> routineTask = StartAsNewTaskAsync(routineFunc, state, cancelToken, routineSyncCtx);
            return new WorkflowRoutine<TResult>(routineTask, routineSyncCtx);
        }

        private static Task<TResult> StartAsNewTaskAsync<TArg, TResult>(Func<TArg, CancellationToken, Task<TResult>> routineFunc,
                                                                        TArg state,
                                                                        CancellationToken cancelToken,
                                                                        WorkflowSynchronizationContext routineSyncCtx)
        {
            Program.WriteLine($"--{nameof(WorkflowRoutine)}.StartAsNewTaskAsync(.., state='{state}', ..): 1");

            if (cancelToken.IsCancellationRequested)
            {
                Program.WriteLine($"--{nameof(WorkflowRoutine)}.StartAsNewTaskAsync(.., state='{state}', ..): End D (End with EARLY Cancellation)");
                return Task<TResult>.FromCanceled<TResult>(cancelToken);
            }

            // Create 'routineTaskFactory' that schedules Tasks using WorkflowSynchronizationContextTaskScheduler
            // associated with the specified 'routineSyncCtx'.
            // Then use 'routineTaskFactory' to create and start a new Task wrapping 'routineFunc'.
            // That will execute 'routineFunc' asynchronously (later) under WorkflowSynchronizationContext.
            // At this point, the created Task wrapping the invocation of 'routineFunc' will be queued,
            // but NOT executed UNTIL we eventually call InvokePostedWorkActions(..).
            // All exceptions that can be observed here (except Cancellation) are related to the process
            // of scheduling and do not originate from the actual 'routineFunc'. Thus, we must not attempt to
            // catch exceptions and embed them into the 'routineTask' (however, that DOES need to happen in 'Execute(..)').

            try
            {
                TaskFactory<Task<TResult>> routineTaskFactory = routineSyncCtx.CreateNewTaskFactory<Task<TResult>>(cancelToken);

                TaskExecutionContext<TArg, TResult> taskExecCtx = new(routineFunc, state, cancelToken);
                Task<Task<TResult>> routineStartTask = routineTaskFactory.StartNew(RoutineTaskExecutorFuncCache<TArg, TResult>.Func,
                                                                                   taskExecCtx,
                                                                                   cancelToken);

                Program.WriteLine($"--{nameof(WorkflowRoutine)}.StartAsNewTaskAsync(.., state='{state}', ..): 2 (routineStartTaskId={routineStartTask.Id})");

                // 'routineStartTask' represents the "starting" of 'routineFunc'. I.e. it "contains" the code of
                // the static 'Execute(..)' helpers below AND the *synchronous* part of 'routineFunc'.
                // That is, if 'routineFunc' completes entirely synchronously, then all of its code will run
                // within the scope of 'routineStartTask's action.
                // If 'routineFunc' contains async await points, then the only the initial (i.e. the synchronous) part 
                // of 'routineFunc' will run within the scope of 'routineStartTask's action.

                if (routineStartTask.IsCompleted)
                {
                    Task<TResult> routineTask = routineStartTask.GetAwaiter().GetResult();

                    Program.WriteLine($"--{nameof(WorkflowRoutine)}.StartAsNewTaskAsync(.., state='{state}', ..): End A (routineTask={routineTask.Id})");
                    return routineTask;
                }
                else
                {
                    Task<TResult> routineProxyTask = routineStartTask.Unwrap();

                    Program.WriteLine($"--{nameof(WorkflowRoutine)}.StartAsNewTaskAsync(.., state='{state}', ..): End B (routineProxyTask={routineProxyTask.Id})");
                    return routineProxyTask;
                }
            }
            catch (OperationCanceledException ocEx)
            {
                if (ocEx.CancellationToken == cancelToken)
                {
                    Program.WriteLine($"--{nameof(WorkflowRoutine)}.StartAsNewTaskAsync(.., state='{state}', ..): End C (End with INNER Cancellation ({ocEx.GetType().Name}))");
                    return Task<TResult>.FromCanceled<TResult>(cancelToken);
                }

                ExceptionDispatchInfo.Capture(ocEx).Throw();
                throw;  // This line is never reached.
            }
        }

        private static Task<TResult> Execute<TArg, TResult>(object taskExecutionContextObject)
        {
            if (taskExecutionContextObject == null)
            {
                throw new ArgumentNullException(nameof(taskExecutionContextObject));
            }

            if (!(taskExecutionContextObject is TaskExecutionContext<TArg, TResult> taskExecutionContext))
            {
                throw new ArgumentException($"The specified {nameof(taskExecutionContextObject)} was expected to be of type"
                                          + $" {nameof(TaskExecutionContext<TArg, TResult>)}, but the actual type"
                                          + $" was {taskExecutionContextObject.GetType().FullName}.");
            }

            return Execute<TArg, TResult>(taskExecutionContext);
        }

        /// <summary>
        /// This method invokes
        /// <code>
        ///   Task{TResult} routineTask = taskExecCtx.RoutineFunc(taskExecCtx.UserState, taskExecCtx.CancelToken);
        /// </code>
        /// and returns <c>routineTask</c>.<br />
        /// The returned <c>routineTask</c> represents the eventual completion of <c>taskExecCtx.RoutineFunc</c>.<br />
        /// This method returns as soon as <c>RoutineFunc</c> reached the first await point (if any), it does not
        /// schedule any code to run after <c>RoutineFunc</c> completes.
        /// 
        /// <para>If cancellation has been requested for the specified <c>taskExecCtx.CancelToken</c> before
        /// the <c>RoutineFunc</c> is invoked, then <c>RoutineFunc</c> will NOT be invoked.<br />
        /// Instead, this method will then return a completed Task representing that cancellation.</para>
        /// 
        /// <para>Note: This method returns a Task that represents the completion of <c>taskExecCtx.RoutineFunc</c>,
        /// even if an exception escapes from <c>RoutineFunc</c>:<br />
        /// If <c>taskExecCtx.RoutineFunc</c> throws AFTER the first await point, the exception will be automatically embedded
        /// into <c>routineTask</c>. Such an exception will not be thrown until the <c>routineTask</c> itself is awaited at
        /// some point.<br />
        /// If, however, <c>taskExecCtx.RoutineFunc</c> throws BEFORE it reaches the first await point (or if it never
        /// awaits internally), then the exception will propagate right away.<br />
        /// In this method we catch such exceptions and wrap them into the returned Task.</para>
        /// 
        /// <para>Remember: This method is invoked under the appropriate instance of <see cref="WorkflowSynchronizationContext" />:<br />
        /// We got here by starting a new Task using the TaskFactory obtained from the current
        /// <see cref="WorkflowRoutine{TResult}" />'s <see cref="WorkflowSynchronizationContext" />. That factory has used
        /// the synchronization context's <see cref="WorkflowSynchronizationContextTaskScheduler" />. And that Task Scheduler
        /// has ensured that we are called under the correct synchronization context.</para>
        /// 
        /// </summary>        
        private static Task<TResult> Execute<TArg, TResult>(TaskExecutionContext<TArg, TResult> taskExecCtx)
        {
            Program.WriteLine($"--{nameof(WorkflowRoutine)}.Execute(.., state='{taskExecCtx.UserState}', ..): 1");

            if (taskExecCtx.CancelToken.IsCancellationRequested)
            {
                Program.WriteLine($"--{nameof(WorkflowRoutine)}.Execute(.., state='{taskExecCtx.UserState}', ..): End B  (End with EARLY Cancellation)");
                return Task<TResult>.FromCanceled<TResult>(taskExecCtx.CancelToken);
            }

            try
            {
                Task<TResult> routineTask = taskExecCtx.RoutineFunc(taskExecCtx.UserState, taskExecCtx.CancelToken);

                Program.WriteLine($"--{nameof(WorkflowRoutine)}.Execute(.., state='{taskExecCtx.UserState}', ..): End  (routineTaskId={routineTask.Id})");
                return routineTask;
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException ocEx && ocEx.CancellationToken == taskExecCtx.CancelToken)
                {
                    Program.WriteLine($"--{nameof(WorkflowRoutine)}.Execute(.., state='{taskExecCtx.UserState}', ..): End C  (End with INNER Cancellation ({ex.GetType().Name}))");
                    return Task<TResult>.FromCanceled<TResult>(taskExecCtx.CancelToken);
                }

                Program.WriteLine($"--{nameof(WorkflowRoutine)}.Execute(.., state='{taskExecCtx.UserState}', ..): End D  (End with {ex.GetType().Name}: '{ex.Message}')");
                return Task<TResult>.FromException<TResult>(ex);
            }
        }
    }  // public static class WorkflowRoutine

    public class WorkflowRoutine<TResult>
    {
        private const string ErrMsgEverythingIsCompleted = "This operation is invalid because this"
                                                         + nameof(WorkflowRoutine)
                                                         + "has finished and completed all remaining work actions.";

        private Task<TResult> _routineTask;
        private WorkflowSynchronizationContext _routineSyncCtx;
        private WeakReference<WorkflowSynchronizationContext> _wrRoutineSyncCtx;

        public WorkflowRoutine(Task<TResult> routineTask, WorkflowSynchronizationContext routineSyncCtx)
        {
            _routineTask = routineTask;
            _routineSyncCtx = routineSyncCtx;            
        }

        public bool IsCompleted
        {
            get { return Task.IsCompleted; }
        }

        public Task<TResult> Task
        {
            get { return _routineTask; }
        }

        public bool IsNoWorkRemaining
        {
            get { return (false == TryGetRoutineSyncContext(out _)); }
        }

        public void ExecutePostedWorkActions(bool breakOnYield = true)
        {
            if (TryGetRoutineSyncContext(out WorkflowSynchronizationContext routineSyncCtx))
            {
                routineSyncCtx.ExecutePostedWorkActions(breakOnYield);
            }            
        }

        public Task WhenWorkActionsScheduledAsync()
        {
            if (TryGetRoutineSyncContext(out WorkflowSynchronizationContext routineSyncCtx))
            {
                return routineSyncCtx.WhenWorkActionsScheduledAsync();
            }

            throw new InvalidOperationException(ErrMsgEverythingIsCompleted);
        }

        public void ExecutePostedWorkActionsWhenScheduled(bool breakOnYield = true)
        {
            WhenWorkActionsScheduledAsync().GetAwaiter().GetResult();
            ExecutePostedWorkActions(breakOnYield);
        }

        public async Task ExecutePostedWorkActionsWhenScheduledAsync(bool breakOnYield = true)
        {
            await WhenWorkActionsScheduledAsync();
            ExecutePostedWorkActions(breakOnYield);
        }

        public string ToSynchronizationContextString()
        {
            if (TryGetRoutineSyncContext(out WorkflowSynchronizationContext routineSyncCtx))
            {
                return routineSyncCtx.ToString();
            }

            return String.Empty;
        }

        private bool TryGetRoutineSyncContext(out WorkflowSynchronizationContext routineSyncCtx)
        {
            routineSyncCtx = _routineSyncCtx;
            if (routineSyncCtx != null)
            {
                return true;
            }

            WeakReference<WorkflowSynchronizationContext> wrRoutineSyncCtx = _wrRoutineSyncCtx;
            if (wrRoutineSyncCtx != null && wrRoutineSyncCtx.TryGetTarget(out routineSyncCtx))
            {
                return (routineSyncCtx != null);
            }

            return false;
        }

        public async Task RunToCompletionAsync(bool useThreadPool, CancellationToken cancelToken)
        {
            bool completed = await ((useThreadPool)
                                        ? TaskType.Run( () => RunTillMainRoutineCompletionAsync(cancelToken), cancelToken )
                                        : RunTillMainRoutineCompletionAsync(cancelToken));
            
            if (!completed)
            {
                cancelToken.ThrowIfCancellationRequested();
                throw new InvalidOperationException("Not completed and not canceled should not happen.");
            }
        }

        /// It may be that the <c>_routineTask</c> completed (<see cref="IsCompleted" /> returns <c>true</c>), but some
        /// continuations are still pending to be executed on the <c>_routineSyncCtx</c>. If we do not keep pumping this
        /// WorkflowRoutine's message loop those continuations can never run. So we will keep pumping that loop
        /// (asynchronously) until we loose the weak ref to the sync context, indicating that noone aims to run on that
        /// context any longer.
        public Task CompleteAllRemainingWorkActionsAsync(CancellationToken cancelToken)
        {
            if (IsNoWorkRemaining)
            {
                return TaskType.CompletedTask;
            }

            if (cancelToken.IsCancellationRequested)
            {
                return TaskType.FromCanceled(cancelToken);
            }

            TaskCompletionSource<bool> overallCompletion = new();
            TaskType.Run( () => ExecuteMessageLoopUntilFinishedAsync(overallCompletion, cancelToken) );
            return overallCompletion.Task;
        }

        private async Task ExecuteMessageLoopUntilFinishedAsync(TaskCompletionSource<bool> overallCompletion, CancellationToken cancelToken)
        {
            Program.WriteLine($"--{nameof(WorkflowRoutine)}.ExecuteMessageLoopUntilFinishedAsync: 1");

            WeakReference<WorkflowSynchronizationContext> wrRoutineSyncCtx = null;
            try
            {
                Program.WriteLine($"--{nameof(WorkflowRoutine)}.ExecuteMessageLoopUntilFinishedAsync: 2");

                // Ensure the main routine task is completed:
                await RunTillMainRoutineCompletionAsync(cancelToken);

                Program.WriteLine($"--{nameof(WorkflowRoutine)}.ExecuteMessageLoopUntilFinishedAsync: 3");

                // Respect cancellation:
                if (cancelToken.IsCancellationRequested)
                {
                    overallCompletion.TrySetCanceled(cancelToken);
                    return;
                }

                Program.WriteLine($"--{nameof(WorkflowRoutine)}.ExecuteMessageLoopUntilFinishedAsync: 4");

                // Set '_routineTask' to a completed Task that does not further reference anything,
                // set '_routineSyncCtx' to null, set '_wrRoutineSyncCtx' to what '_routineSyncCtx' previously pointed to,
                // get '_wrRoutineSyncCtx':
                wrRoutineSyncCtx = ClearPrivateInstanceHardRefs(overallCompletion);
                if (overallCompletion.Task.IsCompleted)
                {
                    return;
                }

                Program.WriteLine($"--{nameof(WorkflowRoutine)}.ExecuteMessageLoopUntilFinishedAsync: 5");

                // Respect cancellation:
                if (cancelToken.IsCancellationRequested)
                {
                    overallCompletion.TrySetCanceled(cancelToken);
                    return;
                }

                Program.WriteLine($"--{nameof(WorkflowRoutine)}.ExecuteMessageLoopUntilFinishedAsync: 6");

                // Now pump the underlying Sync Context's message loop until we loose the reference to it:

                try
                {
                    await RunTillSyncContextCollectedAsync(overallCompletion, wrRoutineSyncCtx, cancelToken);
                }
                catch (OperationCanceledException ocEx)
                {
                    if (ocEx.CancellationToken == cancelToken)
                    {                        
                        overallCompletion.TrySetCanceled(cancelToken);
                        return;
                    }

                    ExceptionDispatchInfo.Capture(ocEx).Throw();
                }

                Program.WriteLine($"--{nameof(WorkflowRoutine)}.ExecuteMessageLoopUntilFinishedAsync: 7");
            }
            catch (Exception ex)
            {
                Program.WriteLine($"--{nameof(WorkflowRoutine)}.ExecuteMessageLoopUntilFinishedAsync: 8");
                TryRestoreRoutineSyncCtxRef(wrRoutineSyncCtx);
                overallCompletion.TrySetException(ex);
            }

            Program.WriteLine($"--{nameof(WorkflowRoutine)}.ExecuteMessageLoopUntilFinishedAsync: End");
        }

        private async Task<bool> RunTillMainRoutineCompletionAsync(CancellationToken cancelToken)
        {
            Program.WriteLine($"--{nameof(WorkflowRoutine)}.RunTillMainRoutineCompletionAsync: 1 (Compltd={IsCompleted}, Canceld={cancelToken.IsCancellationRequested})");

            while (!IsCompleted && !cancelToken.IsCancellationRequested)
            {
                await TaskType.WhenAny(this.Task, WhenWorkActionsScheduledAsync());

                Program.WriteLine($"--{nameof(WorkflowRoutine)}.RunTillMainRoutineCompletionAsync: 2 (Compltd={IsCompleted}, Canceld={cancelToken.IsCancellationRequested})");

                if (!cancelToken.IsCancellationRequested)
                {
                    Program.WriteLine($"--{nameof(WorkflowRoutine)}.RunTillMainRoutineCompletionAsync: 3 (Compltd={IsCompleted}, Canceld={cancelToken.IsCancellationRequested})");
                    ExecutePostedWorkActions(breakOnYield: false);
                }

                Program.WriteLine($"--{nameof(WorkflowRoutine)}.RunTillMainRoutineCompletionAsync: 4 (Compltd={IsCompleted}, Canceld={cancelToken.IsCancellationRequested})");
            }

            bool isCompleted = IsCompleted;

            Program.WriteLine($"--{nameof(WorkflowRoutine)}.RunTillMainRoutineCompletionAsync: End (Compltd={IsCompleted}, Canceld={cancelToken.IsCancellationRequested})");
            return isCompleted;
        }

        /// <summary>
        /// - Set <c>_routineTask</c> to a completed Task that does not further reference anything.
        /// - Set <c>_routineSyncCtx</c> to null.
        /// - Ensure that <c>_wrRoutineSyncCtx</c> points to the instance that <c>_routineSyncCtx</c> previously pointed to.
        /// - Return a reference to <c>_wrRoutineSyncCtx</c>.
        /// </summary>        
        private WeakReference<WorkflowSynchronizationContext> ClearPrivateInstanceHardRefs(TaskCompletionSource<bool> overallCompletion)
        {
            Program.WriteLine($"--{nameof(WorkflowRoutine)}.ClearPrivateInstanceHardRefs: 1  (routineTaskId={_routineTask.Id}, routineSyncCtx={_routineSyncCtx?.ToString() ?? "null"})");

            // Clear the reference to the routine's Task by copying the results into a new instance:

            Task<TResult> fixedTask;
            try
            {
                TResult routineResult = _routineTask.Result;
                fixedTask = TaskType.FromResult(routineResult);
            }
            catch (OperationCanceledException ocEx)
            {
                fixedTask = TaskType.FromCanceled<TResult>(ocEx.CancellationToken);
            }
            catch (Exception ex)
            {
                fixedTask = TaskType.FromException<TResult>(ex);
            }

            _ = Interlocked.Exchange(ref _routineTask, fixedTask);

            Program.WriteLine($"--{nameof(WorkflowRoutine)}.ClearPrivateInstanceHardRefs: 2  (routineTaskId={_routineTask.Id}, routineSyncCtx={_routineSyncCtx?.ToString() ?? "null"})");

            // Install a Weak Ref to the _routineSyncCtx:

            if (!TryGetRoutineSyncContext(out WorkflowSynchronizationContext routineSyncCtx))
            {
                // Cannot get routineSyncCtx means a concurrent call to this method has already done everything.
                Program.WriteLine($"--{nameof(WorkflowRoutine)}.ClearPrivateInstanceHardRefs: End B  (routineTaskId={_routineTask.Id}, routineSyncCtx={_routineSyncCtx?.ToString() ?? "null"})");
                overallCompletion.TrySetResult(true);
                return null;
            }            

            WeakReference<WorkflowSynchronizationContext> wrRoutineSyncCtx = Volatile.Read(ref _wrRoutineSyncCtx);
            if (wrRoutineSyncCtx == null)
            {
                Program.WriteLine($"--{nameof(WorkflowRoutine)}.ClearPrivateInstanceHardRefs: 3  (routineTaskId={_routineTask.Id}, routineSyncCtx={_routineSyncCtx?.ToString() ?? "null"})");

                wrRoutineSyncCtx = new WeakReference<WorkflowSynchronizationContext>(routineSyncCtx, trackResurrection: false);
                WeakReference<WorkflowSynchronizationContext> prevWR = Interlocked.CompareExchange(ref _wrRoutineSyncCtx, wrRoutineSyncCtx, null);
                wrRoutineSyncCtx = prevWR ?? wrRoutineSyncCtx;
            }

            // Clear the hard reference to the _routineSyncCtx:

            Volatile.Write(ref _routineSyncCtx, null);
            
            Program.WriteLine($"--{nameof(WorkflowRoutine)}.ClearPrivateInstanceHardRefs:End  (routineTaskId={_routineTask.Id}, routineSyncCtx={_routineSyncCtx?.ToString() ?? "null"})");
            return wrRoutineSyncCtx;
        }

        private bool TryRestoreRoutineSyncCtxRef(WeakReference<WorkflowSynchronizationContext> wrRoutineSyncCtx)
        {
            if (wrRoutineSyncCtx != null && wrRoutineSyncCtx.TryGetTarget(out WorkflowSynchronizationContext routineSyncCtx))
            {
                Volatile.Write(ref _routineSyncCtx, routineSyncCtx);
                Program.WriteLine($"--{nameof(WorkflowRoutine)}.TryRestoreRoutineSyncCtxRef: Restored");
                return true;
            }

            return false;
        }

        private static async Task RunTillSyncContextCollectedAsync(TaskCompletionSource<bool> overallCompletion,
                                                                   WeakReference<WorkflowSynchronizationContext> wrRoutineSyncCtx,
                                                                   CancellationToken cancelToken)
        {
            int[] delayMsec = { 20, 100, 500, 1000, 1000, 1000, 5000, 5000 };
            int delayInd = 0;

            Program.WriteLine($"--{nameof(WorkflowRoutine)}.RunTillSyncContextCollectedAsync: 1");

            while (true)
            {
                Program.WriteLine($"--{nameof(WorkflowRoutine)}.RunTillSyncContextCollectedAsync: 2");

                cancelToken.ThrowIfCancellationRequested();

                // Pump the message loop:
                bool canAccessRoutineSyncCtx = TryExecutePostedWorkActionsViaWeakRef(wrRoutineSyncCtx);

                if (!canAccessRoutineSyncCtx)
                {
                    // If we lost the weak ref, then we are done.
                    Program.WriteLine($"--{nameof(WorkflowRoutine)}.RunTillSyncContextCollectedAsync: End");
                    overallCompletion.TrySetResult(true);
                    return;
                }

                Program.WriteLine($"--{nameof(WorkflowRoutine)}.RunTillSyncContextCollectedAsync: 3");

                // Run GC and sleep waiting for any new work.
                GC.Collect();

                await TaskType.Delay(delayMsec[delayInd], cancelToken);
                delayInd = (delayInd + 1) % delayMsec.Length;

                Program.WriteLine($"--{nameof(WorkflowRoutine)}.RunTillSyncContextCollectedAsync: 4 (delayInd={delayInd})");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool TryExecutePostedWorkActionsViaWeakRef(WeakReference<WorkflowSynchronizationContext> wrRoutineSyncCtx)
        {
            if (wrRoutineSyncCtx.TryGetTarget(out WorkflowSynchronizationContext routineSyncCtx))
            {
                routineSyncCtx.ExecutePostedWorkActions(breakOnYield: false);
                Volatile.Write(ref routineSyncCtx, null);
                return true;
            }

            Volatile.Write(ref routineSyncCtx, null);
            return false;
        }
    }
}
