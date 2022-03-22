using System;
using System.Text;
using System.Threading.Tasks;
using Candidly.Util;
using Google.Protobuf;
using Grpc.Core;
using Temporal.Api.Common.V1;
using Temporal.Api.Enums.V1;
using Temporal.Api.History.V1;
using Temporal.Api.TaskQueue.V1;
using Temporal.Api.Workflow.V1;
using Temporal.Api.WorkflowService.V1;

#if NETCOREAPP
using Microsoft.Extensions.Logging;
#endif

namespace Temporal.Demos.AdHocScenarios
{
    internal class UseRawGrpcClient
    {
        private const string TemporalServerHost = "localhost";
        private const int TemporalServerPort = 7233;


        public void Run()
        {
            Console.WriteLine();

            ListWorkflowExecutionsAsync().GetAwaiter().GetResult();
            StartWorkflowAsync().GetAwaiter().GetResult();
            WaitForWorkflowAsync().GetAwaiter().GetResult();

            Console.WriteLine();
            Console.WriteLine("Done. Press Enter.");
            Console.ReadLine();
        }

        private WorkflowService.WorkflowServiceClient CreateClientNetFx()
        {
#if NETFRAMEWORK
            Grpc.Core.Channel channel = new Grpc.Core.Channel(TemporalServerHost, TemporalServerPort, ChannelCredentials.Insecure);

            WorkflowService.WorkflowServiceClient client = new(channel);
            return client;
#else
            throw new NotSupportedException("This routine is only supported on Net Fx.");
#endif
        }

        private WorkflowService.WorkflowServiceClient CreateClientNetCore()
        {
#if NETCOREAPP
            if (!RuntimeEnvironmentInfo.SingletonInstance.CoreAssembyInfo.IsSysPrivCoreLib)
            {
                throw new InvalidOperationException("RuntimeEnvironmentInfo.SingeltonInstance.CoreAssembyInfo.IsSysPrivCoreLib was expected to be True.");
            }

            if (RuntimeEnvironmentInfo.SingletonInstance.RuntimeVersion.StartsWith("3"))
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            }

            ILoggerFactory logFactory = LoggerFactory.Create((logBuilder) =>
            {
                ConsoleLoggerExtensions.AddConsole(logBuilder);
                logBuilder.SetMinimumLevel(LogLevel.Trace);
            });

            Grpc.Net.Client.GrpcChannel channel = Grpc.Net.Client.GrpcChannel.ForAddress($"http://{TemporalServerHost}:{TemporalServerPort}",
                                                                                         new Grpc.Net.Client.GrpcChannelOptions()
                                                                                         {
                                                                                             //LoggerFactory = logFactory
                                                                                         });

            WorkflowService.WorkflowServiceClient client = new(channel);
            return client;
#else
            throw new NotSupportedException("This routine is only supported on Net Core and Net 5+.");
#endif
        }

        private WorkflowService.WorkflowServiceClient CreateClient()
        {
            return RuntimeEnvironmentInfo.SingletonInstance.CoreAssembyInfo.IsMscorlib
                        ? CreateClientNetFx()
                        : CreateClientNetCore();
        }

        public async Task ListWorkflowExecutionsAsync()
        {
            Console.WriteLine("\n----------- Workflow ListWorkflowExecutionsAsync { ----------- -----------\n");

            WorkflowService.WorkflowServiceClient client = CreateClient();

            ListWorkflowExecutionsRequest reqListExecs = new()
            {
                Namespace = "default",
            };

            ListWorkflowExecutionsResponse resListExecs = await client.ListWorkflowExecutionsAsync(reqListExecs);

            foreach (WorkflowExecutionInfo weInfo in resListExecs.Executions)
            {
                Console.WriteLine($"WorkflowId=\"{weInfo.Execution.WorkflowId}\";"
                                + $" RunId=\"{weInfo.Execution.RunId}\";"
                                + $" TypeName=\"{weInfo.Type.Name}\""
                                + $" Status=\"{weInfo.Status}\"");
            }

            Console.WriteLine();
            Console.WriteLine("----------- } Workflow ListWorkflowExecutionsAsync ----------- -----------");
            Console.WriteLine();
        }

        public async Task StartWorkflowAsync()
        {
            Console.WriteLine("\n----------- Workflow StartWorkflowAsync { ----------- -----------\n");

            WorkflowService.WorkflowServiceClient client = CreateClient();

            StartWorkflowExecutionRequest reqStartWf = new()
            {
                Namespace = "default",
                WorkflowId = "Some-Workflow-Id",
                WorkflowType = new WorkflowType()
                {
                    Name = "Some-Workflow-Id",
                },
                TaskQueue = new TaskQueue()
                {
                    Name = "Some-Task-Queue",
                },
                RequestId = Guid.NewGuid().ToString(),
            };

            AsyncUnaryCall<StartWorkflowExecutionResponse> calStartWf = client.StartWorkflowExecutionAsync(reqStartWf);

            try
            {
                StartWorkflowExecutionResponse resStartWf = await calStartWf;
                Console.WriteLine($"Workflow Execution started. RunId = \"{resStartWf.RunId}\".");
            }
            catch (RpcException rpcEx)
            {
                Console.WriteLine($"Could not start Workflow Execution. {rpcEx}");
            }

            Console.WriteLine("\n----------- } Workflow StartWorkflowAsync ----------- -----------\n");
        }

        public async Task WaitForWorkflowAsync()
        {
            const string DemoWorkflowId = "Some-Workflow-Id";

            Console.WriteLine();
            Console.WriteLine("\n----------- Workflow WaitForWorkflowAsync { ----------- -----------\n");

            try
            {
                await WaitForWorkflowAsync(DemoWorkflowId, workflowRunId: String.Empty);
                Console.WriteLine($"Workflow result received successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting workflow result. {ex}");
            }

            Console.WriteLine("\n----------- } Workflow WaitForWorkflowAsync ----------- -----------\n");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0010:Add missing cases", Justification = "Only need to process terminal events.")]
        private async Task WaitForWorkflowAsync(string workflowId, string workflowRunId)
        {
            WorkflowService.WorkflowServiceClient client = CreateClient();

            const string ServerCallDescription = "GetWorkflowExecutionHistoryAsync(..) with HistoryEventFilterType = CloseEvent";
            ByteString nextPageToken = ByteString.Empty;

            // Spin and retry or follow the workflow chain until we get a result, or hit a non-retriable error, or time out:
            while (true)
            {

                GetWorkflowExecutionHistoryRequest reqGWEHist = new()
                {
                    Namespace = "default",
                    Execution = new WorkflowExecution()
                    {
                        WorkflowId = workflowId,
                        RunId = workflowRunId
                    },
                    NextPageToken = nextPageToken,
                    WaitNewEvent = true,
                    HistoryEventFilterType = HistoryEventFilterType.CloseEvent,
                };

                GetWorkflowExecutionHistoryResponse resGWEHist = await client.GetWorkflowExecutionHistoryAsync(reqGWEHist);

                if (resGWEHist.History.Events.Count == 0 && resGWEHist.NextPageToken != null && resGWEHist.NextPageToken.Length > 0)
                {
                    Console.WriteLine("Received no history events, but a non-empty NextPageToken. Repeating call.");
                    nextPageToken = resGWEHist.NextPageToken;
                    continue;
                }

                if (resGWEHist.History.Events.Count != 1)
                {
                    throw new UnexpectedServerResponseException(ServerCallDescription,
                                                                nameof(WaitForWorkflowAsync),
                                                                $"History.Events.Count was expected to be 1, but it was in fact {resGWEHist.History.Events.Count}");
                }

                HistoryEvent historyEvent = resGWEHist.History.Events[0];

                switch (historyEvent.EventType)
                {
                    case EventType.WorkflowExecutionCompleted:
                        Console.WriteLine("Workflow completed succsessfully.");
                        return;

                    case EventType.WorkflowExecutionFailed:
                        Console.WriteLine($"Workflow execution failed ({historyEvent.WorkflowExecutionFailedEventAttributes.Failure.Message}).");
                        return;

                    case EventType.WorkflowExecutionTimedOut:
                        Console.WriteLine($"Workflow execution timed out.");
                        return;

                    case EventType.WorkflowExecutionCanceled:
                        Console.WriteLine($"Workflow execution cancelled.");
                        return;

                    case EventType.WorkflowExecutionTerminated:
                        Console.WriteLine($"Workflow execution terminated (reason=\"{historyEvent.WorkflowExecutionTerminatedEventAttributes.Reason}\").");
                        return;

                    case EventType.WorkflowExecutionContinuedAsNew:
                        string nextRunId = historyEvent.WorkflowExecutionContinuedAsNewEventAttributes.NewExecutionRunId;

                        if (String.IsNullOrWhiteSpace(nextRunId))
                        {
                            throw new UnexpectedServerResponseException(ServerCallDescription,
                                                                        nameof(WaitForWorkflowAsync),
                                                                        $"EventType was WorkflowExecutionContinuedAsNew, but NewExecutionRunId=\"{nextRunId}\"");
                        }

                        Console.WriteLine($"Workflow execution continued as new. Following workflow chain to RunId=\"{nextRunId}\"");
                        workflowRunId = nextRunId;
                        continue;

                    default:
                        throw new UnexpectedServerResponseException(ServerCallDescription,
                                                                    nameof(WaitForWorkflowAsync),
                                                                    $"Unexpected History EventType (\"{historyEvent.EventType}\")");
                }
            }  // while(true)
        }

        public class UnexpectedServerResponseException : Exception
        {
            private static string FormatMessage(string serverCall, string scenario, string problemDescription)
            {
                StringBuilder message = new();

                if (!String.IsNullOrWhiteSpace(problemDescription))
                {
                    message.Append(problemDescription);
                }

                if (!String.IsNullOrWhiteSpace(problemDescription))
                {
                    if (message.Length > 0 && message[message.Length - 1] != '.')
                    {
                        if (message[message.Length - 1] != '.')
                        {
                            message.Append('.');
                        }

                        message.Append(' ');
                    }

                    message.Append("Server Call: \"");
                    message.Append(serverCall);
                    message.Append("\".");
                }

                if (!String.IsNullOrWhiteSpace(scenario))
                {
                    if (message.Length > 0 && message[message.Length - 1] != '.')
                    {
                        if (message[message.Length - 1] != '.')
                        {
                            message.Append('.');
                        }

                        message.Append(' ');
                    }

                    message.Append("Scenario: \"");
                    message.Append(scenario);
                    message.Append("\".");
                }

                return message.ToString();
            }

            public UnexpectedServerResponseException(string serverCall, string scenario, string problemDescription)
                : base(FormatMessage(serverCall, scenario, problemDescription))
            {
            }

            public UnexpectedServerResponseException(string serverCall, string scenario, Exception unexpectedException)
                : base(FormatMessage(serverCall, scenario, $"Unexpected exception occurred ({unexpectedException.GetType().Name})"))
            {
            }
        }
    }
}
