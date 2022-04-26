using System;
using System.Threading.Tasks;
using Temporal.Util;
using Temporal.WorkflowClient;
using Temporal.WorkflowClient.OperationConfigurations;

namespace Temporal.Sdk.WorkflowClient.UsageSamples
{
    public class Part1_SimpleClientUsage
    {
        public void Run()
        {
            Console.WriteLine($"\n{this.GetType().Name}.{nameof(Run)}(..) started.\n");

            RunAsync().GetAwaiter().GetResult();

            Console.WriteLine();
            Console.WriteLine($"\n{this.GetType().Name}.{nameof(Run)}(..) finished.\nPress Enter.");
            Console.ReadLine();
        }

        public async Task RunAsync()
        {
            Console.WriteLine($"\n{this.GetType().Name}.{nameof(RunAsync)}(..) started.\n");

            Console.WriteLine($"\nExecuting {nameof(CreateConnectedWorkflowClientAsync)}...");
            await CreateConnectedWorkflowClientAsync();

            Console.WriteLine($"\nExecuting {nameof(CreateUnconnectedWorkflowClient)}...");
            CreateUnconnectedWorkflowClient();

            Console.WriteLine($"\n{this.GetType().Name}.{nameof(RunAsync)}(..) finished.\n");
        }

        public async Task CreateConnectedWorkflowClientAsync()
        {
            // The connection and server capabilities will be espablished / obtained eagerly
            // when client is created.

            // For local host (using default port and namespace):
            {
                ITemporalClient workflowClient = await TemporalClient.ConnectAsync(TemporalClientConfiguration.ForLocalHost());
                UseClient(workflowClient);
            }

            // For Temporal cloud:
            // @ToDo.

            // For hosted cluster (unsecured):
            {
                TemporalClientConfiguration clientConfig = new()
                {
                    Namespace = "SomeNamespace",
                    ServiceHost = "some.host.name",
                };

                ITemporalClient workflowClient = await TemporalClient.ConnectAsync(clientConfig);
                UseClient(workflowClient);
            }

            // For hosted cluster (TLS):
            // @ToDo.
        }

        public void CreateUnconnectedWorkflowClient()
        {
            // The connection and server capabilities will be espablished / obtained lazily
            // when the first remote call is invoked on the client.

            // For local host (using default port and namespace):
            {
                ITemporalClient workflowClient = new TemporalClient(TemporalClientConfiguration.ForLocalHost());
                UseClient(workflowClient);
            }

            // For Temporal cloud:
            // @ToDo.

            // For hosted cluster (unsecured):
            {
                TemporalClientConfiguration clientConfig = new()
                {
                    Namespace = "SomeNamespace",
                    ServiceHost = "some.host.name",
                };

                ITemporalClient workflowClient = new TemporalClient(clientConfig);
                UseClient(workflowClient);
            }

            // For hosted cluster (TLS):
            // @ToDo.
        }

        public record SomeWorkflowInputA();

        public async Task StartNewWorkflowAsync()
        {
            ITemporalClient client = ObtainClient();

            // Workflow with no arguments:
            {

                using IWorkflowHandle workflow = await client.StartWorkflowAsync("Sample-Workflow-Id",
                                                                                 "Sample-Workflow-Type-Name",
                                                                                 "Sample-Task-Queue");
                UseWorkflow(workflow);
            }


            // Workflow with some input of any type (here, we use `SomeWorkflowInputA`):
            {
                SomeWorkflowInputA wfInput = new();
                using IWorkflowHandle workflow = await client.StartWorkflowAsync("Sample-Workflow-Id",
                                                                                 "Sample-Workflow-Type-Name",
                                                                                 "Sample-Task-Queue",
                                                                                 wfInput);
                UseWorkflow(workflow);
            }

            // Workflow additional configuration:
            {
                SomeWorkflowInputA wfInput = new();
                using IWorkflowHandle workflow = await client.StartWorkflowAsync("Sample-Workflow-Id",
                                                                                 "Sample-Workflow-Type-Name",
                                                                                 "Sample-Task-Queue",
                                                                                 wfInput,
                                                                                 new StartWorkflowConfiguration()
                                                                                 {
                                                                                     WorkflowExecutionTimeout = TimeSpan.FromMinutes(5),
                                                                                     WorkflowTaskTimeout = TimeSpan.FromMinutes(1)
                                                                                 });
                UseWorkflow(workflow);
            }
        }

        #region --- Helpers ---

        private void UseClient(ITemporalClient workflowClient)
        {
            Validate.NotNull(workflowClient);
            // Do stuff with `workflowClient`.
        }

        private void UseWorkflow(IWorkflowHandle workflow)
        {
            Validate.NotNull(workflow);
            // Do stuff with `workflow`.
        }

        private ITemporalClient ObtainClient()
        {
            TemporalClientConfiguration clinetConfig = TemporalClientConfiguration.ForLocalHost();
            return new TemporalClient(clinetConfig);
        }

        #endregion --- Helpers ---
    }
}
