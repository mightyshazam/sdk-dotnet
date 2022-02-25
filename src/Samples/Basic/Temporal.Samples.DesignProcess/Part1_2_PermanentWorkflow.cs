using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using Temporal.Common.DataModel;
using Temporal.Worker.Activities;
using Temporal.Worker.Hosting;
using Temporal.Worker.Workflows;

namespace Temporal.Sdk.BasicSamples
{
    public class Part1_2_PermanentWorkflow
    {
        [Workflow(runMethod: nameof(SpeakUntilCancelledAsync))]
        public class SayGreetingWorkflow
        {            
            private const int SingleWorkflowRunIterations = 10000;
            private const string AddresseeNameDefault = "Boss";

            private TaskCompletionSource<string> _setAddresseeName = new TaskCompletionSource<string>();
            
            public async Task SpeakUntilCancelledAsync(SpeechRequest greetingUtterance, IWorkflowContext workflowCtx)
            {
                try
                {
                    for (int i = 0; i < SingleWorkflowRunIterations; i++)
                    {
                        string addresseeName = await GetAddresseeNameAsync();

                        var greeting = new SpeechRequest($"{greetingUtterance.Text ?? "Hello"}, {addresseeName ?? AddresseeNameDefault}.");
                        await workflowCtx.Activities.ExecuteAsync(RemoteApiNames.Activities.SpeakGreeting, greeting);
                    }
                }
                catch(TaskCanceledException tcEx)
                {
                    if (tcEx.Task == _setAddresseeName.Task)
                    {
                        return;
                    }

                    ExceptionDispatchInfo.Capture(tcEx).Throw();
                }

                workflowCtx.ConfigureContinueAsNew(startNewRunAfterReturn: true, greetingUtterance);
            }

            private async Task<string> GetAddresseeNameAsync()
            {
                string addresseeName = await _setAddresseeName.Task;
                _setAddresseeName = new TaskCompletionSource<string>();
                return addresseeName;
            }

            [WorkflowSignalHandler]
            public void SetAddressee(SpeechRequest input)
            {
                _setAddresseeName.TrySetResult(input?.Text);                
            }

            [WorkflowSignalHandler]
            public void Cancel()
            {
                while (!_setAddresseeName.TrySetCanceled())
                {
                    _setAddresseeName = new TaskCompletionSource<string>();
                }                
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

                        serviceCollection.AddWorkflowWithAttributes<SayGreetingWorkflow>();

                        serviceCollection.AddActivity<SpeechRequest>(RemoteApiNames.Activities.SpeakGreeting, Speak.GreetingAsync);
                    })
                    .Build();

            appHost.Run();
        }
    }
}
