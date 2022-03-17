using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using Temporal.Common.DataModel;
using Temporal.Worker.Activities;
using Temporal.Worker.Hosting;
using Temporal.Worker.Workflows;

namespace Temporal.Sdk.BasicSamples
{
    public class Part1_3_TimersAndComposition
    {
        // This sample is equivalent to:
        // https://github.com/temporalio/samples-java/blob/main/src/main/java/io/temporal/samples/hello/HelloPeriodic.java

        [Workflow(mainMethod: nameof(GreetRegularlyAsync), WorkflowTypeName = RemoteApiNames.GreetingWorkflow.TypeName)]
        public class HelloPeriodicWorkflow
        {
            private const int SchedulePeriodTargetSecs = 5;
            private const int SchedulePeriodVariationSecs = 2;

            private const int SingleWorkflowRunIterations = 10000;
            private const string AddresseeNameDefault = "Boss";

            private readonly TaskCompletionSource<bool> _requestExit = new TaskCompletionSource<bool>();

            public async Task GreetRegularlyAsync(SpeechRequest nameInfo, IWorkflowContext workflowCtx)
            {
                string name = nameInfo?.Text ?? AddresseeNameDefault;
                Random rnd = workflowCtx.DeterministicApi.CreateNewRandom();

                workflowCtx.ConfigureContinueAsNew(startNewRunAfterReturn: true, nameInfo);

                for (int i = 0; i < SingleWorkflowRunIterations; i++)
                {
                    int delayMillis = (SchedulePeriodTargetSecs * 1000)
                                      + rnd.Next(SchedulePeriodVariationSecs * 1000)
                                      - (SchedulePeriodVariationSecs * 500);

                    await SpeakGreetingAsync($"Hello {name}! I will sleep for {delayMillis} milliseconds and then I will greet you again.",
                                             workflowCtx);

                    Task sleepTask = workflowCtx.SleepAsync(TimeSpan.FromMilliseconds(delayMillis));
                    await Task.WhenAny(_requestExit.Task, sleepTask);

                    if (_requestExit.Task.IsCompleted)
                    {
                        await SpeakGreetingAsync($"Hello {name}! It was requested to quit the periodic greetings, so this the last one.",
                                                 workflowCtx);

                        workflowCtx.ConfigureContinueAsNew(startNewRunAfterReturn: false);
                        return;
                    }
                }
            }
            
            private Task SpeakGreetingAsync(string greetingText, IWorkflowContext workflowCtx)
            {
                return workflowCtx.Activities.ExecuteAsync(RemoteApiNames.Activities.SpeakGreeting, new SpeechRequest(greetingText));
            }

            [WorkflowSignalHandler]
            public void RequestExit()
            {
                _requestExit.TrySetResult(true);
            }
        }

        /// <summary>A sample activity implementation.</summary>
        public static class Speak
        {
            public static Task GreetingAsync(SpeechRequest input, WorkflowActivityContext activityCtx)
            {
                Console.WriteLine($"[{activityCtx.ActivityTypeName}] {input.Text}");
                return Task.CompletedTask;
            }
        }

        public static class RemoteApiNames
        {
            public static class Activities
            {
                public const string SpeakGreeting = "SpeakAGreeting";
            }

            public static class GreetingWorkflow
            {                
                public const string TypeName = "GreetingWorkflow";
            }
        }

        /// <summary>
        /// Parameters to workflow APIs (main method, signal & query parameters) and to activities must implement <see cref="IDataValue" />.
        /// In some specialized cases where it is not possible, the raw (non-deserialized) payload may be accessed
        /// directly (e.g. <see cref="Part4_1_BasicWorkflowUsage" /> and <see cref="Part4_2_BasicWorkflowUsage_MultipleWorkers" />).
        /// </summary>
        public class SpeechRequest : IDataValue
        {
            public string Text { get; set; }

            public SpeechRequest(string text)
            {
                Text = text;
            }
        }

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

                        serviceCollection.AddWorkflowWithAttributes<HelloPeriodicWorkflow>();

                        serviceCollection.AddActivity<SpeechRequest>(RemoteApiNames.Activities.SpeakGreeting, Speak.GreetingAsync);
                    })
                    .Build();

            appHost.Run();
        }
    }
}
