using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using Temporal.Common.DataModel;
using Temporal.Worker.Activities;
using Temporal.Worker.Hosting;
using Temporal.Worker.Workflows;

namespace Temporal.Sdk.BasicSamples
{
    public class Part1_1_TypicalWorkflow
    {
        [Workflow(mainMethod: nameof(SayHelloAsync))]
        public class SayHelloWorkflow
        {
            private const string AddresseeNameDefault = "Boss";

            private string _addresseeName = null;

            public async Task SayHelloAsync(IWorkflowContext workflowCtx)
            {
                var greeting = new SpeechRequest($"Hello, {_addresseeName ?? AddresseeNameDefault}.");                
                await workflowCtx.Activities.ExecuteAsync("SpeakAGreeting", greeting);
            }

            [WorkflowSignalHandler]
            public Task SetAddresseeAsync(SpeechRequest input, IWorkflowContext _)
            {
                _addresseeName = input?.Text;
                return Task.CompletedTask;                
            }
        }

        /// <summary>
        /// Parameters to workflow APIs (main method, signal & query parameters) and to activities must implement <see cref="IDataValue" />.
        /// In some specialized cases where it is not possible, the raw (non-deserialized) payload may be accessed
        /// directly (e.g. <see cref="Part4_1_BasicWorkflowUsage" /> and <see cref="Part4_2_BasicWorkflowUsage_MultipleWorkers" />).
        /// </summary>
        public class SpeechRequest : IDataValue
        {
            public SpeechRequest(string text)
            {
                Text = text;
            }

            public string Text
            {
                get; set;
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

                serviceCollection.AddWorkflowWithAttributes<SayHelloWorkflow>();

                serviceCollection.AddActivity<SpeechRequest>("SpeakAGreeting", Speak.GreetingAsync);
            })
            .Build();

    appHost.Run();
}
    }
}
