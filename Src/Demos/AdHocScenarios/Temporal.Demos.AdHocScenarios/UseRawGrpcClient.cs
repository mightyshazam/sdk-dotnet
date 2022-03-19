using System;
using System.Threading.Tasks;
using Candidly.Util;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Temporal.Api.Common.V1;
using Temporal.Api.TaskQueue.V1;
using Temporal.Api.Workflow.V1;
using Temporal.Api.WorkflowService.V1;

namespace Temporal.Demos.AdHocScenarios
{
    internal class UseRawGrpcClient
    {
        public void Run()
        {
            if (RuntimeEnvironmentInfo.SingeltonInstance.RuntimeName.Equals(".NET Framework", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("\n\n");
                Console.WriteLine($" !!!");
                Console.WriteLine($" !!! Running on the classic .NET Framework (ver {RuntimeEnvironmentInfo.SingeltonInstance.RuntimeVersion}).");
                Console.WriteLine($" !!! This is currently not (yet) supported. Things are likely to break.");
                Console.WriteLine($" !!! For now, use .NET Core or .NET 5+ instead.");
                Console.WriteLine($" !!!");
                Console.WriteLine("\n\n");
            }

            ListWorkflowExecutionsAsync().GetAwaiter().GetResult();
            RunAsync().GetAwaiter().GetResult();

            Console.WriteLine();
            Console.WriteLine("Done. Press Enter.");
            Console.ReadLine();
        }

        public async Task ListWorkflowExecutionsAsync()
        {
            Console.WriteLine();
            Console.WriteLine("----------- Workflow Executions { ----------- -----------");

            if (RuntimeEnvironmentInfo.SingeltonInstance.RuntimeName.Equals(".NET Core", StringComparison.OrdinalIgnoreCase)
                    && RuntimeEnvironmentInfo.SingeltonInstance.RuntimeVersion.StartsWith("3"))
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            }

            ILoggerFactory logFactory = LoggerFactory.Create((logBuilder) =>
                {
                    logBuilder.AddConsole();
                    logBuilder.SetMinimumLevel(LogLevel.Trace);
                });

            using GrpcChannel channel = GrpcChannel.ForAddress(address: "http://localhost:7233",
                                                               new GrpcChannelOptions()
                                                               {
                                                                   //LoggerFactory = logFactory
                                                                   //,
                                                                   //HttpHandler = new WinHttpHandler(),
                                                                   //HttpHandler = new GrpcWebHandler(new HttpClientHandler())
                                                                   //HttpHandler = new GrpcWebHandler(new WinHttpHandler())
                                                               });

            WorkflowService.WorkflowServiceClient client = new(channel);

            ListWorkflowExecutionsRequest reqListExecs = new()
            {
                Namespace = "default",
            };

            ListWorkflowExecutionsResponse resListExecs = await client.ListWorkflowExecutionsAsync(reqListExecs);

            foreach(WorkflowExecutionInfo weInfo in resListExecs.Executions)
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

            GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:7233");
            WorkflowService.WorkflowServiceClient client = new(channel);

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
