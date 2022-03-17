using System;
using System.Threading;
using System.Threading.Tasks;

namespace ControlTaskContinuationOrder
{
    public class Program
    {
        internal static void Main(string[] _)
        {
            WriteLine("Hello World!");
            (new Program()).Execute();
        }

        private static object s_writeLineLock = new();
        private static int s_lastWriteLineThreadId = -1;

        public static void WriteLine(string msg = null)
        {
            lock (s_writeLineLock)
            {
                string lnBreak = String.Empty;

                int currentThreadId = Thread.CurrentThread.ManagedThreadId;
                if (s_lastWriteLineThreadId != currentThreadId)
                {
                    lnBreak = Environment.NewLine + "-----------" + Environment.NewLine;
                }

                msg = msg ?? String.Empty;

                string syncCtxStr = (SynchronizationContext.Current == null)
                                        ? "null"
                                        : (typeof(WorkflowSynchronizationContext) == SynchronizationContext.Current.GetType())
                                                ? SynchronizationContext.Current.ToString()
                                                : SynchronizationContext.Current.GetType().Name;

                string tskSchedStr = (TaskScheduler.Current == null)
                                        ? "null"
                                        : (typeof(WorkflowSynchronizationContextTaskScheduler) == TaskScheduler.Current.GetType())
                                                ? TaskScheduler.Current.ToString()
                                                : $"{TaskScheduler.Current.Id}|{TaskScheduler.Current.GetType().Name}";

                string timeStamp = DateTime.Now.ToString("mm:ss.fffffff");

                Console.WriteLine($"{lnBreak}"
                                + Environment.NewLine
                                + $"[{timeStamp};"
                                + $" Thread={currentThreadId};"
                                + $" TaskId={Task.CurrentId};"
                                + $" SyncCtx={syncCtxStr};"
                                + $" TskSched={tskSchedStr}]"
                                + " "
                                + Environment.NewLine
                                + $"{msg}");

                s_lastWriteLineThreadId = currentThreadId;
            }
        }
       

        private TaskCompletionSource<string> _signalReceivedCompletion = new TaskCompletionSource<string>();
        private TaskCompletionSource<string> _activityCompletion = null;
        private string _activityName = null;
        private bool _phaseBReached = false;
        private Task<Task<string>> _signalScheduledTask = null;

        private Task _signalHandler2LoopCompletionTask = null;

        public void Execute()
        {
            WriteLine($"==== Execute OUTER: 1");

            ExecuteInner();

            WriteLine($"==== Execute OUTER: 2");

            GC.Collect();

            WriteLine($"==== Execute OUTER: 3");

            _signalHandler2LoopCompletionTask.GetAwaiter().GetResult();

            WriteLine($"==== Execute OUTER: End");
        }

        private void ExecuteInner()
        {
            WriteLine($"==== Execute: 1");

            //WorkflowSynchronizationContext wfSyncCtx = new();
            //SynchronizationContext wfSyncCtx = new();
            //SynchronizationContext.SetSynchronizationContext(wfSyncCtx);

            WriteLine($"==== Execute: 2");

            WorkflowRoutine<bool> wfMain = WorkflowRoutine.Start(WorkflowMainAsync);

            WriteLine($"wfMain-Sync-Ctx: {wfMain.ToSynchronizationContextString()}");
            WriteLine($"==== Execute: 3");

            wfMain.ExecutePostedWorkActions(breakOnYield: true);

            WriteLine($"==== Execute: 4");

            WorkflowRoutine<WorkflowRoutine.Void> activityCompleter = WorkflowRoutine.Start(CompleteActivityHelper);
            WriteLine($"activityCompleter-Sync-Ctx: {activityCompleter.ToSynchronizationContextString()}");

            WriteLine($"==== Execute: 5");

            while (!activityCompleter.IsCompleted)
            {
                WriteLine($"==== Execute: 6.1");
                activityCompleter.ExecutePostedWorkActionsWhenScheduled();
                WriteLine($"==== Execute: 6.2");
            }

            WriteLine($"==== Execute: 7");

            WorkflowRoutine<WorkflowRoutine.Void> signalHandler = WorkflowRoutine.Start(SignalHandler, "Signal A received");
            WriteLine($"signalHandler-Sync-Ctx: {signalHandler.ToSynchronizationContextString()}");

            WriteLine($"==== Execute: 8");

            //wfMain.InvokeAllPostedAsyncItems();

            WriteLine($"==== Execute: 9");

            while (!signalHandler.IsCompleted)
            {
                WriteLine($"==== Execute: 10.1");
                signalHandler.ExecutePostedWorkActionsWhenScheduled();
                WriteLine($"==== Execute: 10.2");
            }

            while (!wfMain.IsCompleted && !_phaseBReached)
            {
                WriteLine($"==== Execute: 11.1");
                wfMain.ExecutePostedWorkActionsWhenScheduled();
                WriteLine($"==== Execute: 11.2");
            }

            WriteLine($"==== Execute: 12");

            WorkflowRoutine<WorkflowRoutine.Void> signalHandler2 = WorkflowRoutine.Start(SignalHandler2, "Signal B received");
            WriteLine($"signalHandler2-Sync-Ctx: {signalHandler2.ToSynchronizationContextString()}");

            WriteLine($"==== Execute: 13");
            
            signalHandler2.RunToCompletionAsync(useThreadPool: true, CancellationToken.None).GetAwaiter().GetResult();

            WriteLine($"==== Execute: 14");

            wfMain.ExecutePostedWorkActionsWhenScheduled();

            WriteLine($"==== Execute: 15");

            Volatile.Write(ref _signalScheduledTask, null);

            _signalHandler2LoopCompletionTask = signalHandler2.CompleteAllRemainingWorkActionsAsync(CancellationToken.None);

            WriteLine($"==== Execute: 16");

            while (!wfMain.IsCompleted)
            {
                WriteLine($"==== Execute: 17.1");
                wfMain.ExecutePostedWorkActionsWhenScheduled();
                WriteLine($"==== Execute: 17.2");
            }

            WriteLine($"==== Execute: 18");

            wfMain.Task.GetAwaiter().GetResult();
            WriteLine($"==== Execute: End");
        }

        private static bool TryGetTaskFactory<TResult>(out TaskFactory<TResult> taskFactory)
        {
            taskFactory = null;

            SynchronizationContext currSyncCtx = SynchronizationContext.Current;
            if (currSyncCtx == null)
            {
                return false;
            }

            if (! (currSyncCtx is WorkflowSynchronizationContext wfSyncCtx))
            {
                return false;
            }

            taskFactory = wfSyncCtx.CreateNewTaskFactory<TResult>(CancellationToken.None);
            return true;
        }

        private async Task<bool> WorkflowMainAsync()
        {
            WriteLine($"#### WorkflowMainAsync: 1");

            Task<string> activityTask = ScheduleActivityAsync("Activity 1");

            WriteLine($"#### WorkflowMainAsync: 2");

            await activityTask;

            WriteLine($"#### WorkflowMainAsync: 2.5");

            Task<string> completedTask = await Task.WhenAny(activityTask, _signalReceivedCompletion.Task);

            WriteLine($"#### WorkflowMainAsync: 3");

            string value1 = await completedTask;

            WriteLine($"#### WorkflowMainAsync: 4");
            WriteLine($"#### WorkflowMainAsync: value1=\"{value1}\".");

            WriteLine($"#### WorkflowMainAsync:"
                    + $" activityTask.Status={activityTask.Status};"
                    + $" _signalReceivedCompletion.Task.Status={_signalReceivedCompletion.Task.Status}");

            await Task.Yield();

            WriteLine($"#### WorkflowMainAsync: 5");

            Task<Task<string>> scheduledTask;
            if (TryGetTaskFactory(out TaskFactory<Task<string>> taskFactory))
            {
                WriteLine($"#### WorkflowMainAsync: 5.1");

                scheduledTask = taskFactory.StartNew( async () =>
                    {
                        Program.WriteLine($"#### WorkflowMainAsync-scheduledTask: 1");
                        await Task.Delay(1000);
                        Program.WriteLine($"#### WorkflowMainAsync-scheduledTask: End");
                        return "scheduledTask Completed";
                    });

                WriteLine($"#### WorkflowMainAsync: 5.2");
            }
            else
            {
                WriteLine($"#### WorkflowMainAsync: 5.3");
                scheduledTask = Task.FromResult(Task.FromResult("#### WorkflowMainAsync-scheduledTask: Could not obtain TaskFactory."));
                WriteLine($"#### WorkflowMainAsync: 5.4");
            }

            WriteLine($"#### WorkflowMainAsync: 6");

            Task<string> scheduledTaskTask = await scheduledTask;

            WriteLine($"#### WorkflowMainAsync: 7");

            string scheduledTaskResult = await scheduledTaskTask;

            WriteLine($"#### WorkflowMainAsync: 8");
            WriteLine($"#### WorkflowMainAsync: scheduledTaskResult=\"{scheduledTaskResult}\"");

            await Task.Yield();

            WriteLine($"#### WorkflowMainAsync: 9");

            string[] value2 = await Task.WhenAll(activityTask, _signalReceivedCompletion.Task);

            WriteLine($"#### WorkflowMainAsync: 10");
            WriteLine($"#### WorkflowMainAsync: value2=\"{value2}\".");

            WriteLine($"#### WorkflowMainAsync:"
                    + $" activityTask.Status={activityTask.Status};"
                    + $" _signalReceivedCompletion.Task.Status={_signalReceivedCompletion.Task.Status}");

            WriteLine($"#### WorkflowMainAsync: 11");

            _phaseBReached = true;
            await Task.Yield();

            Task<Task<string>> signalScheduledTask = _signalScheduledTask;

            if (signalScheduledTask == null)
            {
                WriteLine($"#### WorkflowMainAsync: 12");
            }
            else
            {
                WriteLine($"#### WorkflowMainAsync: 13");
                WriteLine($"#### WorkflowMainAsync: signalScheduledTaskId={signalScheduledTask.Id}");

                Task<string> signalScheduledTaskTask = await signalScheduledTask;

                WriteLine($"#### WorkflowMainAsync: 14");
                WriteLine($"#### WorkflowMainAsync: signalScheduledTaskTaskId={signalScheduledTaskTask.Id}");

                string signalScheduledTaskResult = await signalScheduledTaskTask;

                WriteLine($"#### WorkflowMainAsync: 15");
                WriteLine($"#### WorkflowMainAsync: signalScheduledTaskResult='{signalScheduledTaskResult}'");
            }

            WriteLine($"#### WorkflowMainAsync: End");
            return true;
        }

        private Task<string> ScheduleActivityAsync(string name)
        {
            WriteLine($"$$$$ ScheduleActivityAsync(\"{name}\"): 1");

            _activityName = name;
            _activityCompletion = new TaskCompletionSource<string>();
            
            WriteLine($"$$$$ ScheduleActivityAsync(\"{name}\"): End");

            return _activityCompletion.Task;
        }

        private async Task SignalHandler(string data)
        {
            WriteLine($"@@@@ SignalHandler(\"{data}\"): 1");

            await Task.Yield();

            WriteLine($"@@@@ SignalHandler(\"{data}\"): 2");

            _signalReceivedCompletion.TrySetResult(data);

            WriteLine($"@@@@ SignalHandler(\"{data}\"): 3");

            await Task.Yield();

            WriteLine($"@@@@ SignalHandler(\"{data}\"): End");
        }

        private void CompleteActivityHelper()
        {
            WriteLine($"%%%% CompleteActivityHelper: 1");

            TaskCompletionSource<string> activityCompletion = Interlocked.Exchange(ref _activityCompletion, null);

            if (activityCompletion == null)
            {
                WriteLine($"%%%% CompleteActivityHelper: _activityCompletion was NULL.");
            }
            else
            {
                WriteLine($"%%%% CompleteActivityHelper: completING activity named \"{_activityName}\".");
                activityCompletion.TrySetResult(_activityName);
                WriteLine($"%%%% CompleteActivityHelper: completED activity named \"{_activityName}\".");
            }

            WriteLine($"%%%% CompleteActivityHelper: End");
        }

        private async Task SignalHandler2(string data)
        {
            WriteLine($"&&&& SignalHandler2(\"{data}\"): 1");

            await Task.Yield();

            WriteLine($"&&&& SignalHandler2(\"{data}\"): 2");
            
            if (TryGetTaskFactory(out TaskFactory<Task<string>> taskFactory))
            {
                WriteLine($"&&&& SignalHandler2: 3.1");

                _signalScheduledTask = taskFactory.StartNew(async () =>
                {
                    Program.WriteLine($"&&&& SignalHandler2-scheduledTask: 1");
                    await Task.Delay(2000);
                    Program.WriteLine($"&&&& SignalHandler2-scheduledTask: End");
                    return "SIGNAL ScheduledTask Completed";
                });

                WriteLine($"&&&& SignalHandler2: 3.2");
            }
            else
            {
                WriteLine($"&&&& SignalHandler2: 3.3");
                _signalScheduledTask = Task.FromResult(Task.FromResult("&&&& SignalHandler2-scheduledTask: Could not obtain TaskFactory."));
                WriteLine($"&&&& SignalHandler2: 3.4");
            }

            WriteLine($"&&&& SignalHandler2(\"{data}\"): End");
        }
    }
}
