using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using Temporal.Common.DataModel;
using Temporal.Worker.Activities;
using Temporal.Worker.Hosting;
using Temporal.Worker.Workflows;

namespace Temporal.Sdk.BasicSamples
{
    public class Part1_4_TimersAndComposition2
    {        
        [Workflow(mainMethod: nameof(CountdownAsync))]
        public class CountdownTimer
        {
            private static readonly TimeSpan CountdownStep = TimeSpan.FromSeconds(10);

            private DateTime _targetTimeUtc;

            private TaskCompletionSource<bool> _requestAbort = new TaskCompletionSource<bool>();
            private TaskCompletionSource<DateTime> _updateTarget = new TaskCompletionSource<DateTime>();

            public async Task<CountdownResult> CountdownAsync(TargetTimePayload target, IWorkflowContext workflowCtx)
            {
                _targetTimeUtc = target.UtcDateTime;

                // Create a CancellationTokenSource that is linked to the IWorkflowContext.
                // If the workflow is canceled then this source will also be canceled.
                CancellationTokenSource targetTimerCancelControl = workflowCtx.DeterministicApi.CreateNewCancellationTokenSource();

                // Set up a timer for the target time.
                Task targetTask = workflowCtx.SleepUntilAsync(_targetTimeUtc, targetTimerCancelControl.Token);

                while (true)
                {
                    // Set up a target timer for one countdown step.
                    Task stepTask = workflowCtx.SleepAsync(CountdownStep);

                    // Wait until any of the timers fire or until any of the signals are received.
                    await Task.WhenAny(targetTask, stepTask, _requestAbort.Task, _updateTarget.Task);

                    if (targetTask.IsCompleted)
                    {
                        // If the target time is reached, use an activity to notify and then complete the workflow.
                        await workflowCtx.Activities.ExecuteAsync(RemoteApiNames.Activities.DisplayCompletion);
                        return new CountdownResult(true);
                    }

                    if (_requestAbort.Task.IsCompleted)
                    {
                        // If the workflow is aborted via a signal, quit now.
                        return new CountdownResult(false);
                    }

                    if (stepTask.IsCompleted)
                    {
                        // If a countdown step period has passed, compute and didplay the the number of remaining steps.
                        TimeSpan remainingTime = _targetTimeUtc - workflowCtx.DeterministicApi.DateTimeUtcNow;
                        var remainingSteps = new DisplayRemainingStepsPayload((int) (remainingTime.TotalMilliseconds / CountdownStep.TotalMilliseconds));

                        // We do not want to wait for the result of the DisplayRemainingSteps Activity; we treat it as fire-and-forget.
                        // Thus, we do not need the resulting task.
                        // However, in order for Temporal to actually schedule the Activity, we must complete the current Workflow Task,
                        // so that the Workflow History is updated with the Command to invoke the Activity.
                        // For that to happen we yield to the underlying message loop.
                        // The workflow is immediately ready for continuation and will be resumed using a new Workflow Task shortly.
                        _ = workflowCtx.Activities.ExecuteAsync(RemoteApiNames.Activities.DisplayRemainingSteps, remainingSteps);
                        await Task.Yield();
                    }

                    if (_updateTarget.Task.IsCompleted)
                    {
                        // The user updated the target time using a signal.

                        // Request that the long-term target timer we used so far is cancelled.
                        targetTimerCancelControl.Cancel();

                        // Set up a new target timer with a new cancel control.
                        _targetTimeUtc = await _updateTarget.Task;
                        targetTimerCancelControl = workflowCtx.DeterministicApi.CreateNewCancellationTokenSource();
                        targetTask = workflowCtx.SleepUntilAsync(_targetTimeUtc, targetTimerCancelControl.Token);

                        // Reset the signal notification task.
                        _updateTarget = new TaskCompletionSource<DateTime>();
                    }
                }
            }

            [WorkflowQueryHandler]
            public TargetTimePayload GetCurrentTargetTimeUtc()
            {
                return new TargetTimePayload(_targetTimeUtc);
            }

            [WorkflowSignalHandler]
            public void RequestAbort()
            {
                _requestAbort.TrySetResult(true);
            }

            [WorkflowSignalHandler(SignalTypeName = RemoteApiNames.CountdownTimerWorkflow.Signals.UpdateTargetTime)]
            public void UpdateTarget(TargetTimePayload target)
            {
                _updateTarget.TrySetResult(target.UtcDateTime);
            }
        }

        /// <summary>A sample activity implementation.</summary>
        public static class Display
        {
            public static void RemainingSteps(DisplayRemainingStepsPayload input, WorkflowActivityContext activityCtx)
            {
                Console.WriteLine($"[{activityCtx.ActivityTypeName}] Remaining steps: {input.Steps}.");                
            }

            public static void Completion(WorkflowActivityContext activityCtx)
            {
                Console.WriteLine($"[{activityCtx.ActivityTypeName}] Completed!");                
            }
        }

        public static class RemoteApiNames
        {
            public static class Activities
            {
                public const string DisplayRemainingSteps = "DisplayRemainingSteps";
                public const string DisplayCompletion = "DisplayCompletion";
            }

            public static class CountdownTimerWorkflow
            {                
                public static class Signals
                {
                    public const string UpdateTargetTime = "UpdateTargetTime";
                }
            }
        }
        
        public record TargetTimePayload(DateTime UtcDateTime) : IDataValue;

        public record DisplayRemainingStepsPayload(int Steps) : IDataValue;

        public record CountdownResult(bool IsTargetTimeReached) : IDataValue;


        public static void Main(string[] args)
        {
            // Use the ASP.Net Core host initialization pattern:
            // 'UseTemporalWorkerHost' will configure all the temporal defaults
            // and also apply all the file-based application config files.
            // Configuraton will automatically be persisted via side affects as appropriate before being passed to the workflow implmentation.
            // (Config files are treated elsewhere.)            

            IHost appHost = Host.CreateDefaultBuilder(args)
                    .UseTemporalWorkerHost()
                    .ConfigureServices(serviceCollection =>
                    {
                        serviceCollection.AddTemporalWorker()
                                .Configure(temporalWorkerConfig =>
                                {
                                    temporalWorkerConfig.TaskQueue = "Some Queue";
                                });

                        serviceCollection.AddWorkflowWithAttributes<CountdownTimer>();

                        serviceCollection.AddActivity<DisplayRemainingStepsPayload>(RemoteApiNames.Activities.DisplayRemainingSteps, Display.RemainingSteps);
                        serviceCollection.AddActivity(RemoteApiNames.Activities.DisplayRemainingSteps, Display.Completion);
                    })
                    .Build();

            appHost.Run();
        }
    }
}
