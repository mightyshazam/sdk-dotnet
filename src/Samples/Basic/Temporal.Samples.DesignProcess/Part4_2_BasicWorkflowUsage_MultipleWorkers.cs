using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using Temporal.Common.DataModel;
using Temporal.Worker.Activities;
using Temporal.Worker.Hosting;
using Temporal.Worker.Workflows;
using Temporal.Worker.Workflows.Base;

namespace Temporal.Sdk.BasicSamples
{
    /// <summary>
    /// Some very simple workflows. The level of abstraction is the same as in <see cref="Part4_1_BasicWorkflowUsage" />.
    /// Here, we focus on multiple workers per process.    
    /// </summary>
    public class Part4_2_BasicWorkflowUsage_MultipleWorkers
    {
        public class SayHelloWorkflow : BasicWorkflowBase
        {            
            public override async Task<PayloadsCollection> RunAsync(IWorkflowContext workflowCtx)
            {
                PayloadsCollection input = workflowCtx.CurrentRun.Input;
                string addresseeName = workflowCtx.GetSerializer(input).Deserialize<string>(input);

                string greeting = $"Hello, {addresseeName ?? "<null>"}.";
                await SpeakGreetingAsync(greeting, workflowCtx);

                return PayloadsCollection.Empty;
            }

            private async Task SpeakGreetingAsync(string text, IWorkflowContext workflowCtx)
            {
                PayloadsCollection greetingPayload = workflowCtx.WorkflowImplementationConfig.DefaultPayloadSerializer.Serialize(text);
                await workflowCtx.Activities.ExecuteAsync("SpeakAGreeting1", greetingPayload);
            }
        }

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

        public class SayGoodByeWorkflow : BasicWorkflowBase
        {
            public override async Task<PayloadsCollection> RunAsync(IWorkflowContext workflowCtx)
            {
                PayloadsCollection input = workflowCtx.CurrentRun.Input;
                string addresseeName = workflowCtx.GetSerializer(input).Deserialize<string>(input);

                string greeting = $"Good bye, {addresseeName ?? "<null>"}.";
                await SpeakGreetingAsync(greeting, workflowCtx);

                return PayloadsCollection.Empty;
            }

            private async Task SpeakGreetingAsync(string text, IWorkflowContext workflowCtx)
            {
                await workflowCtx.Activities.ExecuteAsync("SpeakAGreeting2", new SpeechRequest(text));
            }
        }

        public class SayGreetingWorkflow : BasicWorkflowBase
        {
            public override async Task<PayloadsCollection> RunAsync(IWorkflowContext workflowCtx)
            {
                PayloadsCollection input = workflowCtx.CurrentRun.Input;
                string greeting = workflowCtx.GetSerializer(input).Deserialize<string>(input);
                greeting ??= "<null>";

                await workflowCtx.Activities.ExecuteAsync("SpeakAGreeting2", new SpeechRequest(greeting));

                return PayloadsCollection.Empty;
            }
        }

        public class SpeakAGreetingActivity : IBasicActivity
        {
            /// <summary>Optionally customize the name under which this activity is registered:</summary>
            public string ActivityTypeName { get { return "SpeakAGreeting1"; } }
            
            public Task<PayloadsCollection> RunAsync(PayloadsCollection input, WorkflowActivityContext activityCtx)
            {
                string greetingText = activityCtx.GetSerializer(input).Deserialize<string>(input) ?? "<null>";
                Console.WriteLine($"[{ActivityTypeName}] {greetingText}");
                return Task.FromResult<PayloadsCollection>(null);
            }
        }

        public static class Speak
        {
            public static Task GreetingAsync(SpeechRequest input, WorkflowActivityContext activityCtx)
            {
                Console.WriteLine($"[{activityCtx.ActivityTypeName}] {input.Text}");
                return Task.CompletedTask;
            }
        }

        public static void Main_UseMultipleWorkers(string[] args)
        {
            IHost appHost = Host.CreateDefaultBuilder(args)
                    .UseTemporalWorkerHost()
                    .ConfigureServices(serviceCollection =>
                    {
                        WorkerRegistration workerRegistration1 = serviceCollection.AddTemporalWorker()
                                .Configure(temporalWorkerConfiguration =>
                                {
                                    temporalWorkerConfiguration.TaskQueue = "Some Queue";
                                });

                        WorkerRegistration workerRegistration2 = serviceCollection.AddTemporalWorker()
                                .Configure(temporalWorkerConfiguration =>
                                {
                                    temporalWorkerConfiguration.TaskQueue = "Some Other Queue";
                                });

                        // The following workflows and activities will be assigned to the First Worker:

                        // (using  default WorkflowExecutionConfiguration:)
                        serviceCollection.AddWorkflowWithOverrides<SayHelloWorkflow>().AssignWorker(workerRegistration1);

                        serviceCollection.AddActivity<SpeakAGreetingActivity>().AssignWorker(workerRegistration1);


                        // The following workflows and activities will be assigned to the Second Worker:

                        // (use custom WorkflowExecutionConfiguration:)
                        serviceCollection.AddWorkflowWithOverrides<SayGoodByeWorkflow>()
                                .AssignWorker(workerRegistration2)
                                .ConfigureExecution(workflowExecutionConfiguration =>
                                {
                                    workflowExecutionConfiguration.WorkflowTaskTimeoutMillisec = 5_000;
                                });

                        serviceCollection.AddActivity<SpeechRequest>("SpeakAGreeting2", Speak.GreetingAsync)
                                .AssignWorker(workerRegistration2);


                        // Not specifying a token assiciates workflows & activities with ALL workers:

                        serviceCollection.AddWorkflowWithOverrides<SayGreetingWorkflow>();

                        // Nothing prevents us from registering the same implementation several times under different activity names:
                        serviceCollection.AddActivity<SpeechRequest>("SpeakAGreeting3", Speak.GreetingAsync);
                    })
                    .Build();

            appHost.Run();
        }
    }
}
