using System;
using System.Threading.Tasks;
using Candidly.Util;
using Grpc.Core;
using Temporal.Api.Common.V1;
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
            ListWorkflowExecutionsAsync().GetAwaiter().GetResult();
            RunAsync().GetAwaiter().GetResult();

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
            if (!RuntimeEnvironmentInfo.SingeltonInstance.CoreAssembyInfo.IsSysPrivCoreLib)
            {
                throw new InvalidOperationException("RuntimeEnvironmentInfo.SingeltonInstance.CoreAssembyInfo.IsSysPrivCoreLib was expected to be True.");
            }

            if (RuntimeEnvironmentInfo.SingeltonInstance.RuntimeVersion.StartsWith("3"))
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
            return RuntimeEnvironmentInfo.SingeltonInstance.CoreAssembyInfo.IsMscorlib
                        ? CreateClientNetFx()
                        : CreateClientNetCore();
        }

        public async Task ListWorkflowExecutionsAsync()
        {
            Console.WriteLine();
            Console.WriteLine("----------- Workflow Executions { ----------- -----------");

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

            Console.WriteLine("----------- } Workflow Executions ----------- -----------");
        }

        public async Task RunAsync()
        {
            Console.WriteLine();

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
        }
    }
}
