using System;
using System.Threading.Tasks;
using Temporal.Common;
using Temporal.Util;
using Temporal.WorkflowClient;
using Temporal.WorkflowClient.Errors;
using Temporal.WorkflowClient.OperationConfigurations;

namespace Temporal.Sdk.WorkflowClient.UsageSamples
{
    /// <summary>
    /// This Part demonstates the most common usages of the Workflow Client SDK.
    /// </summary>
    /// <remarks>This class contains usage sample intended for education and API review. It may not actually execute
    /// successfully because IDs used in these samples may not actually exist on the backing Temporal server.</remarks>
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

            Console.WriteLine($"\nExecuting {nameof(StartNewWorkflowAsync)}...");
            await StartNewWorkflowAsync();

            Console.WriteLine($"\nExecuting {nameof(AccessExistingWorkflow)}...");
            AccessExistingWorkflow();

            Console.WriteLine($"\nExecuting {nameof(GetWorkflowResultAsync)}...");
            await GetWorkflowResultAsync();

            Console.WriteLine($"\nExecuting {nameof(SignalWorkflowAsync)}...");
            await SignalWorkflowAsync();

            Console.WriteLine($"\nExecuting {nameof(QueryWorkflowAsync)}...");
            await QueryWorkflowAsync();

            Console.WriteLine($"\nExecuting {nameof(CancelWorkflowAsync)}...");
            await CancelWorkflowAsync();

            Console.WriteLine($"\nExecuting {nameof(TerminateWorkflowAsync)}...");
            await TerminateWorkflowAsync();

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
            {
                ITemporalClient workflowClient = await TemporalClient.ConnectAsync(
                                                            TemporalClientConfiguration.ForTemporalCloud(@"SomeNamespace",
                                                                                                         @"PATH\NAME.crt.pem",
                                                                                                         @"PATH\NAME.key.pem"));
                UseClient(workflowClient);
            }

            // For hosted cluster (unsecured):
            {
                TemporalClientConfiguration clientConfig = new()
                {
                    Namespace = "SomeNamespace",
                    ServiceConnection = TemporalClientConfiguration.Connection.TlsDisabled("some-domain.com", 1234),
                };

                ITemporalClient workflowClient = await TemporalClient.ConnectAsync(clientConfig);
                UseClient(workflowClient);
            }

            // For hosted cluster (TLS):
            {
                TemporalClientConfiguration clientConfig = new()
                {
                    Namespace = "SomeNamespace",
                    ServiceConnection = TemporalClientConfiguration.Connection.TlsEnabled("some-domain.com", 1234) with
                    {
                        ServerCertAuthority = TemporalClientConfiguration.TlsCertificate.FromPemFile(@"PATH\ca.crt.pem")
                    }
                };

                ITemporalClient workflowClient = await TemporalClient.ConnectAsync(clientConfig);
                UseClient(workflowClient);
            }
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
            {
                ITemporalClient workflowClient = new TemporalClient(
                                                            TemporalClientConfiguration.ForTemporalCloud(@"SomeNamespace",
                                                                                                         @"PATH\NAME.crt.pem",
                                                                                                         @"PATH\NAME.key.pem"));
                UseClient(workflowClient);
            }

            // For hosted cluster (unsecured):
            {
                TemporalClientConfiguration clientConfig = new()
                {
                    Namespace = "SomeNamespace",
                    ServiceConnection = TemporalClientConfiguration.Connection.TlsDisabled("some-domain.com", 1234),
                };

                ITemporalClient workflowClient = new TemporalClient(clientConfig);
                UseClient(workflowClient);
            }

            // For hosted cluster (TLS):
            {
                TemporalClientConfiguration clientConfig = new()
                {
                    Namespace = "SomeNamespace",
                    ServiceConnection = TemporalClientConfiguration.Connection.TlsEnabled("some-domain.com", 1234) with
                    {
                        ServerCertAuthority = TemporalClientConfiguration.TlsCertificate.FromPemFile(@"PATH\ca.crt.pem")
                    }
                };

                ITemporalClient workflowClient = new TemporalClient(clientConfig);
                UseClient(workflowClient);
            }
        }

        public record SomeWorkflowInput();

        public async Task StartNewWorkflowAsync()
        {
            ITemporalClient client = ObtainClient();

            // Workflow with no arguments:
            {

                using IWorkflowHandle workflow = await client.StartWorkflowAsync(CreateUniqueWorkflowId(),
                                                                                 "Sample-Workflow-Type-Name",
                                                                                 "Sample-Task-Queue");
                UseWorkflow(workflow);
            }


            // Workflow with some input of any type (here, we use `SomeWorkflowInputA`):
            {
                SomeWorkflowInput wfInput = new();
                using IWorkflowHandle workflow = await client.StartWorkflowAsync(CreateUniqueWorkflowId(),
                                                                                 "Sample-Workflow-Type-Name",
                                                                                 "Sample-Task-Queue",
                                                                                 wfInput);
                UseWorkflow(workflow);
            }

            // Workflow additional configuration:
            {
                SomeWorkflowInput wfInput = new();
                using IWorkflowHandle workflow = await client.StartWorkflowAsync(CreateUniqueWorkflowId(),
                                                                                 "Sample-Workflow-Type-Name",
                                                                                 "Sample-Task-Queue",
                                                                                 wfInput,
                                                                                 new StartWorkflowConfiguration()
                                                                                 {
                                                                                     WorkflowExecutionTimeout = TimeSpan.FromMinutes(5),
                                                                                     WorkflowTaskTimeout = TimeSpan.FromMinutes(1),
                                                                                     // . . .
                                                                                 });
                UseWorkflow(workflow);
            }

            // `ITemporalClient.StartWorkflowAsync(..)` throws an exception if workflow is alaready running:
            {
                IWorkflowHandle workflow;

                try
                {
                    workflow = await client.StartWorkflowAsync("Sample-Workflow-Id",
                                                               "Sample-Workflow-Type-Name",
                                                               "Sample-Task-Queue");
                }
                catch (WorkflowAlreadyExistsException wfExistsEx)
                {
                    Console.WriteLine($"A workflow with id \"{wfExistsEx.WorkflowId}\" already exists in namespace \"{wfExistsEx.Namespace}\".");
                    workflow = null;
                }

                if (workflow != null)
                {
                    UseWorkflow(workflow);
                }
            }
        }

        public void AccessExistingWorkflow()
        {
            ITemporalClient client = ObtainClient();

            // In this scenario, we know that a workflow with the workfow-id "Sample-Workflow-Id" already exists.
            {
                using IWorkflowHandle workflow = client.CreateWorkflowHandle("Sample-Workflow-Id");

                UseWorkflow(workflow);
            }
        }

        public record SomeWorkflowOutput();

        public async Task GetWorkflowResultAsync()
        {
            ITemporalClient client = ObtainClient();

            // In this scenario, we know that a workflow with the workfow-id "Sample-Workflow-Id" already exists,
            // and it returns a value of type `SomeWorkflowOutput`:
            {
                using IWorkflowHandle workflow = client.CreateWorkflowHandle("Sample-Workflow-Id");

                SomeWorkflowOutput resultValue = await workflow.GetResultAsync<SomeWorkflowOutput>();
            }

            // In this scenario, we start a new workflow.
            // We know that it returns a value of type `int`:
            {
                using IWorkflowHandle workflow = await client.StartWorkflowAsync(CreateUniqueWorkflowId(),
                                                                                 "Sample-Workflow-Type-Name",
                                                                                 "Sample-Task-Queue");

                int resultValue = await workflow.GetResultAsync<int>();
            }

            // In this scenario, we do not care how the workflow handle is obtained.
            // We do not access the value returned by the workflow, just wait for its conclusion.
            // Note that GetResultAsync<T>(..) will throw for any non-successful conclusion.
            {
                using IWorkflowHandle workflow = await ObtainWorkflowAsync();

                await workflow.GetResultAsync<IPayload.Void>();
            }
        }

        public async Task SignalWorkflowAsync()
        {
            using IWorkflowHandle workflow = await ObtainWorkflowAsync();

            double signalArg = 42.0;
            await workflow.SignalAsync("Signal-Name", signalArg);

            // The Task retuned by SignalAsync(..) completes when the Temporal server persisted the request to send the signal.
            // The handling of the signal by the worker occurs asynchrinously.
        }

        public async Task QueryWorkflowAsync()
        {
            using IWorkflowHandle workflow = await ObtainWorkflowAsync();

            DateTimeOffset queryArg = DateTimeOffset.Now;
            decimal queryResultValue = await workflow.QueryAsync<DateTimeOffset, decimal>("Query-Name", queryArg);
        }

        public async Task CancelWorkflowAsync()
        {
            using IWorkflowHandle workflow = await ObtainWorkflowAsync();

            await workflow.RequestCancellationAsync();

            // The Task retuned by RequestCancellationAsync(..) completes when the Temporal server persisted the request to cancel.
            // The cancellation request will be delivered to the workder asynchronously and the workflow may or many not honour such request.
        }

        public async Task TerminateWorkflowAsync()
        {
            // Simply terminate:
            {
                using IWorkflowHandle workflow = await ObtainWorkflowAsync();

                await workflow.TerminateAsync();
            }

            // Specify a reason:
            {
                using IWorkflowHandle workflow = await ObtainWorkflowAsync();

                await workflow.TerminateAsync("Termination-reason");
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

        private string CreateUniqueWorkflowId()
        {
            return $"Sample-Workflow-Id-{Guid.NewGuid().ToString("D")}";
        }

        private async Task<IWorkflowHandle> ObtainWorkflowAsync()
        {
            ITemporalClient client = ObtainClient();

            string workflowId = CreateUniqueWorkflowId();
            IWorkflowHandle workflow = await client.StartWorkflowAsync(workflowId,
                                                                       "Sample-Workflow-Type-Name",
                                                                       "Sample-Task-Queue");

            return workflow;
        }

        #endregion --- Helpers ---
    }
}
