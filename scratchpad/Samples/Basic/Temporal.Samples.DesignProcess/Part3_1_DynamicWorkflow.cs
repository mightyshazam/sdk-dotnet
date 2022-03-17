using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Temporal.Common.DataModel;
using Temporal.Worker.Hosting;
using Temporal.Worker.Activities;
using Temporal.Worker.Workflows.Dynamic;
using Temporal.Worker.Workflows;

namespace Temporal.Sdk.BasicSamples
{
    /// <summary>
    /// </summary>
    public class Part3_1_DynamicWorkflow
    {
        /// <summary>A very simple dynamic workflow definition toy model.</summary>
        public class CustomWorkflowDefinition : IDataValue
        {
            public static CustomWorkflowDefinition CreateFromJson(string json)
            {
                return JsonSerializer.Deserialize<CustomWorkflowDefinition>(json);
            }

            public class State
            {
                public string Name { get; set; }            // State id
                public string[] Transitions { get; set; }   // Legal transitions from this state. 
            }

            public string CommandNamespace { get; set; }    // Required prefix for all valid commands (e.g. "myWf" -> "myWfQuit" is a valid exit command).
            public string ExitCommand { get; set; }         // Command to finish the workflow (e.g. "Quit").
            public string TransitionCommand { get; set; }   // Required prefix for valid transition commands (e.g. "Move" -> "myWfMoveFoo" = transition to state "Foo").
            public string InitState { get; set; }           // Name of the initial state.
            public State[] States { get; set; }             // States
        }

        /// <summary>Example instance of <see cref="CustomWorkflowDefinition" />.</summary>
        private const string CustomWorkflowJson = "{"
                + "\n    'CommandNamespace': 'demo',"
                + "\n    'ExitCommand': 'Quit',"
                + "\n    'TransitionCommand': 'Move',"
                + "\n    'InitState': 'Abc',"
                + "\n    'States': ["
                + "\n        { 'Name': 'Abc', 'Transitions': ['Xyz', 'Qwerty', 'FooBar'] },"
                + "\n        { 'Name': 'Xyz', 'Transitions': ['Xyz', 'Foobar'] },"
                + "\n        { 'Name': 'Query', 'Transitions': ['Abc', 'FooBar'] },"
                + "\n        { 'Name': 'FooBar', 'Transitions': ['Xyz'] }"
                + "\n    ]"
                + "\n}";

        public class NotifyWorkflowStateEnteredData : IDataValue
        {
            public string State { get; set; }

            public NotifyWorkflowStateEnteredData(string state)
            {
                State = state;
            }
        }

        public class CustomWorkflowExecutor : DynamicWorkflowBase
        {
            private string _transitionPrefix = null;
            private bool _isExitRequested = false;
            private TaskCompletionSource<string> _signalHandled = null;

            public override async Task RunAsync(IDynamicWorkflowContext workflowCtx)
            {
                // Any signal arriving while we are setting up will be cached and processed later:
                workflowCtx.DynamicControl.SignalHandlerDefaultPolicy = SignalHandlerDefaultPolicy.CacheAndProcessWhenHandlerIsSet(".*");

                // Make sure to process the signals in the order they arrive:
                workflowCtx.DynamicControl.SignalHandlingOrderPolicy = SignalHandlingOrderPolicy.Strict();

                // Load the custom workflow definition from a hypothetical storage:
                CustomWorkflowDefinition customWf = await workflowCtx.Activities.ExecuteAsync<CustomWorkflowDefinition>("LoadCustomWorkflowDefinition");
               
                // Calculate the regex pattern for valid state transition signals (valid state names contain letters, numbers and underscores):
                _transitionPrefix = $"{customWf.CommandNamespace}{customWf.TransitionCommand}";                

                // Build state transition table:
                var transitionMatrix = new Dictionary<string, IReadOnlyList<string>>();
                foreach(CustomWorkflowDefinition.State stateSpec in customWf.States)
                {
                    transitionMatrix[stateSpec.Name] = stateSpec.Transitions;
                }

                // Set the initial state:                
                string currentCustomWfState = customWf.InitState;

                // Configure the exit signal handler:
                string customExitCommand = $"{customWf.CommandNamespace}{customWf.ExitCommand}";
                workflowCtx.DynamicControl.SignalHandlers.TryAdd($"^{customExitCommand}$",
                                                                 SignalHandler.Create( () => { _isExitRequested = true; _signalHandled?.SetResult("#"); } ));

                // Setup completed.
                // Loop until exit is requested:

                while (!_isExitRequested)
                {
                    // Dynamically configure the workflow to handle signals expected for the current state:

                    // Remove signal transition handlers for the previous state:
                    ClearTransitionHandlers(workflowCtx);

                    // Add signal transition handlers for the current state:
                    if (transitionMatrix.TryGetValue(currentCustomWfState, out IReadOnlyList<string> validTargetStates))
                    {
                        foreach(string targetState in validTargetStates)
                        {
                            workflowCtx.DynamicControl.SignalHandlers.TryAdd($"^{_transitionPrefix}{targetState}$",
                                                                             SignalHandler.Create( (signalName) => 
                                                                             {
                                                                                 ClearTransitionHandlers(workflowCtx);                                                                                 
                                                                                 _signalHandled.SetResult(targetState);
                                                                             }));
                        }
                    }

                    // Reset the signal for when a signal is handled:
                    _signalHandled = new TaskCompletionSource<string>();

                    // We have now configured the workflow to handle signals expected for the current state.

                    // Now process the current state using a hypothetical activity:

                    var stateData = new NotifyWorkflowStateEnteredData(currentCustomWfState);
                    await workflowCtx.Activities.ExecuteAsync("NotifyWorkflowStateEntered", stateData);

                    // Now wait for the next signal:

                    currentCustomWfState = await _signalHandled.Task;
                }
            }

            private void ClearTransitionHandlers(IDynamicWorkflowContext workflowCtx)
            {
                IHandlerCollection<Func<string, IDataValue, IWorkflowContext, Task>> handlers = workflowCtx.DynamicControl.SignalHandlers;
                int i = handlers.Count - 1;
                while (i >= 0)
                {
                    handlers.GetAt(i, out string matcherRegex, out _);
                    if (matcherRegex.StartsWith($"^{_transitionPrefix}"))
                    {
                        handlers.RemoveAt(i);
                    }

                    i--;
                }
            }
        }

        public class CustomWorkflowFactory
        {
            public Task<CustomWorkflowDefinition> LoadDemoWorkflowAsync(WorkflowActivityContext _)
            {
                return Task.FromResult(CustomWorkflowDefinition.CreateFromJson(CustomWorkflowJson));
            }
        }

        public class WorkflowStateNotificationHandler
        {
            public Task OnWorkflowStateEntered(NotifyWorkflowStateEnteredData stateData, WorkflowActivityContext activityCtx)
            {
                // ToDo: Add stuff to ActivityContext
                //Console.WriteLine($"[{activityCtx.WorkflowInfo}] Entered \"{stateData.State}\".");
                Console.WriteLine($"[...] Entered \"{stateData.State}\".");
                return Task.CompletedTask;
            }
        }

        public static void Main(string[] args)
        {
            IHost appHost = Host.CreateDefaultBuilder(args)
                    .UseTemporalWorkerHost()
                    .ConfigureServices(serviceCollection =>
                    {
                        serviceCollection.AddTemporalWorker()
                                .Configure(temporalWorkerConfiguration =>
                                {
                                    temporalWorkerConfiguration.TaskQueue = "Some Queue";
                                });

                        serviceCollection.AddWorkflowWithOverrides<CustomWorkflowExecutor>();

                        serviceCollection.AddScoped<CustomWorkflowFactory>();
                        serviceCollection.AddScoped<WorkflowStateNotificationHandler>();

                        serviceCollection.AddActivity(
                                "LoadCustomWorkflowDefinition",
                                (serviceProvider) => serviceProvider.GetService<CustomWorkflowFactory>().LoadDemoWorkflowAsync);

                        serviceCollection.AddActivity<NotifyWorkflowStateEnteredData>(
                                "NotifyWorkflowStateEntered",
                                (serviceProvider) => serviceProvider.GetService<WorkflowStateNotificationHandler>().OnWorkflowStateEntered);
                    })
                    .Build();
            appHost.Run();
        }
    }
}
