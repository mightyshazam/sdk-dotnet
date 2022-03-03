using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Temporal.Common.DataModel;
using Temporal.Serialization;
using Temporal.Worker.Hosting;
using Temporal.Worker.Workflows;
using Temporal.Worker.Workflows.Base;
using Temporal.Worker.Activities;

namespace Temporal.Sdk.BasicSamples
{
    /// <summary>
    /// This is the lowest level of a workflow / activity abstraction available to the users.
    /// We will NOT guide them here in any prominent way.
    /// However, users who wish to realize advanced scenarios, may want to use this low-level abstraction.
    /// Examples:
    ///  - a different way to map attributed interfaces or classes to temporal messages
    ///  - a different approach to implementing a dynamic workflow to the one offered by our framework
    ///  For a TYPICAL workflow implementation that we WILL guide users towards, see <see cref="Part1_1_TypicalWorkflow"/>.
    /// </summary>
    public class Part4_1_BasicWorkflowUsage
    {       
        public class SayHelloWorkflow : BasicWorkflowBase
        {
            private const string AddresseeNameDefault = "Boss";

            private string _addresseeName = null;

            public override async Task<PayloadsCollection> RunAsync(IWorkflowContext workflowCtx)
            {
                string greeting = $"Hello, {_addresseeName ?? AddresseeNameDefault}.";

                PayloadsCollection greetingPayload = workflowCtx.WorkflowImplementationConfig.DefaultDataConverter.Serialize(greeting);
                await workflowCtx.Activities.ExecuteAsync("SpeakAGreeting", greetingPayload);

                return PayloadsCollection.Empty;
            }

            public override Task HandleSignalAsync(string signalName, PayloadsCollection input, IWorkflowContext workflowCtx)
            {
                if (signalName.Equals("SetAddressee", StringComparison.OrdinalIgnoreCase))
                {                    
                    string addresseeName = workflowCtx.GetDataConverter().Deserialize<string>(input);
                    
                    _addresseeName = addresseeName;
                    return Task.CompletedTask;
                }

                return base.HandleSignalAsync(signalName, input, workflowCtx);                
            }
        }

        public class SpeakAGreetingActivity : BasicActivityBase
        {
            public override Task<PayloadsCollection> RunAsync(PayloadsCollection input, WorkflowActivityContext activityCtx)
            {
                string greetingText = activityCtx.GetDataConverter().Deserialize<string>(input) ?? "<null>";
                Console.WriteLine($"[{ActivityTypeName}] {greetingText}");
                return Task.FromResult<PayloadsCollection>(null);
            }
        }

        public static void Main(string[] args)
        {
            // 'UseTemporalWorkerHost' will configure all the temporal defaults
            // and also apply all the file-based application config files.
            // Configuraton needs to be persisted via side affects as appropriate.
            // Config files are treated elsewhere.
            // Here we show how to *optionally* tweak configuration through in-line code at different scopes.

            IHost appHost = Host.CreateDefaultBuilder(args)
                    .UseTemporalWorkerHost()
                    .ConfigureServices(serviceCollection =>
                    {
                        serviceCollection.AddTemporalWorker()
                                .Configure(temporalWorkerConfig =>
                                {
                                    temporalWorkerConfig.TaskQueue = "Some Queue";
                                });

                        serviceCollection.AddWorkflowWithOverrides<SayHelloWorkflow>();

                        serviceCollection.AddActivity<SpeakAGreetingActivity>();
                    })
                    .Build();

            appHost.Run();
        }

        public static void Main_Minimal(string[] args)
        {

            IHost appHost = Host.CreateDefaultBuilder(args)
                    .UseTemporalWorkerHost()
                    .ConfigureServices(serviceCollection =>
                    {
                        serviceCollection.AddTemporalWorker();
                        serviceCollection.AddWorkflowWithOverrides<SayHelloWorkflow>();
                        serviceCollection.AddActivity<SpeakAGreetingActivity>();
                    })
                    .Build();

            appHost.Run();
        }

        public static void Main_UseWorkflowExecutionConfiguration(string[] args)
        {
            IHost appHost = Host.CreateDefaultBuilder(args)
                    .UseTemporalWorkerHost()
                    .ConfigureServices(serviceCollection =>
                    {
                        serviceCollection.AddTemporalWorker()
                                .Configure(temporalWorkerConfig =>
                                {
                                    temporalWorkerConfig.TaskQueue = "Some Queue";
                                });

                        serviceCollection.AddWorkflowWithOverrides<SayHelloWorkflow>()
                                .ConfigureExecution(workflowExecutionConfig =>
                                {
                                    workflowExecutionConfig.WorkflowTaskTimeoutMillisec = 5_000;
                                })
                                .ConfigureImplementation((serviceProvider, workflowImplementationConfig) =>
                                {
                                    //workflowImplementationConfig.DefaultDataConverter = serviceProvider.GetService<JsonPayloadSerializer>();

                                    workflowImplementationConfig.DefaultActivityInvocationConfig.TaskQueue = "My Queue";
                                    workflowImplementationConfig.DefaultActivityInvocationConfig.StartToCloseTimeoutMillisecs = 5_000;
                                    workflowImplementationConfig.DefaultActivityInvocationConfig.ScheduleToStartTimeoutMillisecs = 1_000;
                                });

                        serviceCollection.AddActivity<SpeakAGreetingActivity>();
                    })
                    .Build();

            appHost.Run();
        }
        
        public static void Main_UseCustomWorkerConfig(string[] args)
        {
            IHost appHost = Host.CreateDefaultBuilder(args)
                    .UseTemporalWorkerHost(temporalServiceConfig =>
                    {
                        temporalServiceConfig.Namespace = "MyNamespace";
                        temporalServiceConfig.OrchestratorServiceUrl = "http://api.endpoint.com:12345";
                    })
                    .ConfigureServices(serviceCollection =>
                    {
                        serviceCollection.AddTemporalWorker()
                                .Configure(temporalWorkerConfig =>
                                {
                                    temporalWorkerConfig.TaskQueue = "Some Queue";

                                    temporalWorkerConfig.EnablePollForActivities = true;
                                    temporalWorkerConfig.CachedStickyWorkflowsMax = 0;

                                    temporalWorkerConfig.NonStickyQueue.ConcurrentWorkflowTaskPollsMax = 1;
                                    temporalWorkerConfig.NonStickyQueue.ConcurrentActivityTaskPollsMax = 1;

                                    temporalWorkerConfig.StickyQueue.ScheduleToStartTimeoutMillisecs = 10_000;
                                    temporalWorkerConfig.StickyQueue.ConcurrentWorkflowTaskPollsMax = 5;
                                    temporalWorkerConfig.StickyQueue.ConcurrentActivityTaskPollsMax = 5;
                                });

                        serviceCollection.AddWorkflowWithOverrides<SayHelloWorkflow>();
                        serviceCollection.AddActivity<SpeakAGreetingActivity>();
                    })
                    .Build();

            appHost.Run();
        }

        public static void Main_UseCustomDefaultConfig(string[] args)
        {
            IHost appHost = Host.CreateDefaultBuilder(args)
                    .UseTemporalWorkerHost(
                            (hostBuilderCtx, temporalServiceConfig, temporalWorkerConfig, workflowExecutionConfig, workflowImplementationConfig) =>
                            {
                                temporalServiceConfig.Namespace = "MyNamespace";
                                temporalServiceConfig.OrchestratorServiceUrl = "http://api.endpoint.com:12345";

                                temporalWorkerConfig.TaskQueue = "My Queue";
                                temporalWorkerConfig.StickyQueue.ScheduleToStartTimeoutMillisecs = 500;

                                workflowExecutionConfig.WorkflowTaskTimeoutMillisec = 1000;

                                workflowImplementationConfig.DefaultActivityInvocationConfig.TaskQueue = "My Other Queue";
                            })
                    .ConfigureServices(serviceCollection =>
                    {
                        serviceCollection.AddTemporalWorker();
                        serviceCollection.AddWorkflowWithOverrides<SayHelloWorkflow>();
                        serviceCollection.AddActivity<SpeakAGreetingActivity>();
                    })
                    .Build();

            appHost.Run();
        }
    }
}
