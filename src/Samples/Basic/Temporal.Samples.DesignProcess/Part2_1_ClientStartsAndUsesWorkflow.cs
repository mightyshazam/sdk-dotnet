using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Temporal.Common.DataModel;
using Temporal.Common.WorkflowConfiguration;
using Temporal.Serialization;
using Temporal.WorkflowClient;

using static Temporal.Sdk.BasicSamples.Part1_4_TimersAndComposition2;

namespace Temporal.Sdk.BasicSamples
{
    public class Part2_1_ClientStartsAndUsesWorkflow
    {
        public static void Main(string[] args)
        {
            Minimal(args).GetAwaiter().GetResult();
            SpecifyNamespace(args).GetAwaiter().GetResult();
            SpecifyNamespaceAndValidate(args).GetAwaiter().GetResult();
            ValidateConnectionEagerly(args).GetAwaiter().GetResult();
            WorkflowMayAlreadyBeRunning(args).GetAwaiter().GetResult();
            AvoidLongPolls(args).GetAwaiter().GetResult();
            AccessResultOfWorkflow(args).GetAwaiter().GetResult();
            AccessResultOfWorkflowWithNonDataValueResult(args).GetAwaiter().GetResult();
            UseSignalsAndQueries(args).GetAwaiter().GetResult();
            CancelWorkflow(args).GetAwaiter().GetResult();
            UsePayloadCodecToCompressPayloadsForAllWorkflows(args).GetAwaiter().GetResult();
            UsePayloadCodecToCompressPayloadsForSpecificWorkflow(args).GetAwaiter().GetResult();
        }

        public static async Task Minimal(string[] _)
        {
            ITemporalServiceClient serviceClient = new TemporalServiceClient();
            
            IWorkflowConsecution workflowConsecution = await serviceClient.StartNewWorkflowAsync("workflowTypeName", "workflowId", "taskQueue");

            IWorkflowConsecutionResult result = await workflowConsecution.GetResultAsync();
            Console.WriteLine($"Final state: {result.Status}.");
        }

        public static async Task SpecifyNamespace(string[] _)
        {
            TemporalServiceClientConfiguration serviceConfig = new()
            {
                Namespace = "namespace"
            };

            ITemporalServiceClient serviceClient = new TemporalServiceClient(serviceConfig);

            IWorkflowConsecution workflowConsecution = await serviceClient.StartNewWorkflowAsync("workflowTypeName", "workflowId", "taskQueue");

            IWorkflowConsecutionResult result = await workflowConsecution.GetResultAsync();
            Console.WriteLine($"Final state: {result.Status}.");
        }

        public static async Task SpecifyNamespaceAndValidate(string[] _)
        {
            const string RequiredNamespace = "user-namespace";

            ITemporalServiceClient serviceClient = new TemporalServiceClient();
            if (! await serviceClient.TrySetNamespaceAsync(RequiredNamespace))
            {
                Console.WriteLine($"The namespace \"{RequiredNamespace}\" does not exist or is not accessible.");
            }

            IWorkflowConsecution workflowConsecution = await serviceClient.StartNewWorkflowAsync("workflowTypeName", "workflowId", "taskQueue");

            IWorkflowConsecutionResult result = await workflowConsecution.GetResultAsync();
            Console.WriteLine($"Final state: {result.Status}.");
        }

        public static async Task ValidateConnectionEagerly(string[] _)
        {
            TemporalServiceClientConfiguration serviceConfig = new()
            {
                Namespace = "namespace"
            };

            ITemporalServiceClient serviceClient = await TemporalServiceClient.CreateNewAndInitializeConnectionAsync(serviceConfig);
            
            IWorkflowConsecution workflowConsecution = await serviceClient.StartNewWorkflowAsync("workflowTypeName", "workflowId", "taskQueue");

            IWorkflowConsecutionResult result = await workflowConsecution.GetResultAsync();
            Console.WriteLine($"Final state: {result.Status}.");
        }

        public static async Task WorkflowMayAlreadyBeRunning(string[] _)
        {
            ITemporalServiceClient serviceClient = new TemporalServiceClient(new TemporalServiceClientConfiguration() { Namespace = "namespace" });

            IWorkflowConsecution workflowConsecution = await serviceClient.GetOrStartWorkflowAsync("workflowTypeName", "workflowId", "taskQueue");

            await workflowConsecution.GetResultAsync();
            Console.WriteLine("Workflow completed.");
        }

        public static async Task AvoidLongPolls(string[] _)
        {
            ITemporalServiceClient serviceClient = new TemporalServiceClient(new TemporalServiceClientConfiguration() { Namespace = "namespace" });

            IWorkflowConsecution workflowConsecution = await serviceClient.GetWorkflowAsync("workflowId");

            if ((await workflowConsecution.TryGetResultIfAvailableAync()).IsSuccess(out IWorkflowConsecutionResult result))
            {                                
                Console.WriteLine($"Workflow consecution completed with status: {result.Status}.");
            }
            
            Console.WriteLine($"Workflow did not yet complete. Doing some other work...");
            await Task.Delay(TimeSpan.FromSeconds(60));

            if ((await workflowConsecution.TryGetResultIfAvailableAync()).IsSuccess(out result))
            {
                Console.WriteLine($"Workflow consecution completed with status: {result.Status}.");
            }

            Console.WriteLine($"Workflow still did not complete. Good bye.");
        }

        record ComputationResult(int Number) : IDataValue;

        public static async Task AccessResultOfWorkflow(string[] _)
        {
            ITemporalServiceClient serviceClient = new TemporalServiceClient(new TemporalServiceClientConfiguration() { Namespace = "namespace" });

            IWorkflowConsecution workflowConsecution = await serviceClient.StartNewWorkflowAsync("NumbersComputer", "ComputeSomeNumber", "TaskQueue");

            IWorkflowConsecutionResult<ComputationResult> result = await workflowConsecution.GetResultAsync<ComputationResult>();
            Console.WriteLine($"The result of the workflow is {result.Value.Number}.");
        }

        public static async Task AccessResultOfWorkflowWithNonDataValueResult(string[] _)
        {
            ITemporalServiceClient serviceClient = new TemporalServiceClient(new TemporalServiceClientConfiguration() { Namespace = "namespace" });

            IWorkflowConsecution workflowConsecution = await serviceClient.StartNewWorkflowAsync("NumbersComputer", "ComputeSomeNumber", "TaskQueue");

            IWorkflowConsecutionResult result = await workflowConsecution.GetResultAsync();
            if (DataValue.TryUnpack(result.GetValue(), out int resultVal))
            {
                Console.WriteLine($"The result of the workflow is {resultVal}.");
            }
            else
            {
                Console.WriteLine($"Could not obtain the result value because the workflow returned"
                                + $" a value of type {result.GetValue().GetType().Name} where {nameof(Int32)} was expected.");
            }
        }

        public static async Task UseSignalsAndQueries(string[] _)
        {
            ITemporalServiceClient serviceClient = new TemporalServiceClient(new TemporalServiceClientConfiguration() { Namespace = "namespace" });

            // Start countdown timer to finish in 1 min:   
            IWorkflowConsecution workflowConsecution = await serviceClient.StartNewWorkflowAsync(
                                                                                    "CountdownTimer",
                                                                                    "TestTimerXyz",
                                                                                    "taskQueue",
                                                                                    new TargetTimePayload(DateTime.UtcNow.AddMinutes(1)));

            // Do something else...
            await Task.Delay(TimeSpan.FromSeconds(30));

            // Push out target time by 30 secs:
            // Query the workflow, add a minute, signal workflow.
            TargetTimePayload prevTargetTime = (await workflowConsecution.QueryAsync<TargetTimePayload>("GetCurrentTargetTimeUtc")).Value;
            TargetTimePayload newTargetTime = new(prevTargetTime.UtcDateTime.AddSeconds(30));
            await workflowConsecution.SignalAsync(RemoteApiNames.CountdownTimerWorkflow.Signals.UpdateTargetTime, newTargetTime);

            // Wait for the workflow to finish:
            CountdownResult result = (await workflowConsecution.GetResultAsync<CountdownResult>()).Value;
            Console.WriteLine($"The workflow {(result.IsTargetTimeReached ? "did" : "did not")} reach the target time.");
        }

        public static async Task CancelWorkflow(string[] _)
        {
            ITemporalServiceClient serviceClient = new TemporalServiceClient(new TemporalServiceClientConfiguration() { Namespace = "namespace" });

            // Get latest workflow with the specified Id or throw if no workflows with the specified Id can be found:
            IWorkflowConsecution latestConsecution = await serviceClient.GetWorkflowAsync("workflowId");
            
            // Only try to cancel if the fetched workflow is still running:
            if (!await latestConsecution.IsRunningAsync())
            {
                Console.WriteLine("Workflow no longer running");
            }

            // The Task returned by this API is completed when the cancellation request call is completed,
            // i.e. the server persisted the request to cancel into the workflow history.
            // At that time the workflow implementation may not have yet processed the cancellation request.
            // This API will throw if latestConsecution is no longer running which may happen concurrently
            // after the above check.
            await latestConsecution.RequestCancellationAsync();

            // We must await the completion of the workflow to learn whether the remote workflow actually
            // honored the cancellation request.
            // (note that if the workflow does not respect the cancellation, this may await indefinitely)
            IWorkflowConsecutionResult result = await latestConsecution.GetResultAsync();
            Console.WriteLine("Workflow cancellation "
                            + (result.Status == WorkflowExecutionStatus.Canceled ? "was respected" : "was not respected")
                            + " by the workflow.");
        }

        public static async Task UsePayloadCodecToCompressPayloadsForAllWorkflows(string[] _)
        {
            TemporalServiceClientConfiguration serviceConfig = new()
            {
                DataConverterFactory = (_, _, _) => new DefaultDataConverter(new[] { new GZipPayloadCodec() }),
                Namespace = "namespace",
            };

            ITemporalServiceClient serviceClient = new TemporalServiceClient(serviceConfig);
            IWorkflowConsecution workflowConsecution = await serviceClient.StartNewWorkflowAsync("workflowTypeName", "workflowId", "taskQueue");

            await workflowConsecution.GetResultAsync();
        }

        public static async Task UsePayloadCodecToCompressPayloadsForSpecificWorkflow(string[] _)
        {
            ITemporalServiceClient serviceClient = new TemporalServiceClient(new TemporalServiceClientConfiguration() { Namespace = "namespace" });

            WorkflowConsecutionClientConfiguration wfConfig = new() { DataConverter = new DefaultDataConverter(new[] { new GZipPayloadCodec() }) };
            IWorkflowConsecution workflowConsecution = await serviceClient.StartNewWorkflowAsync(
                                                                            "workflowTypeName",
                                                                            "workflowId",
                                                                            new WorkflowExecutionConfiguration() { TaskQueue = "taskQueue"},
                                                                            DataValue.Void,
                                                                            wfConfig,
                                                                            CancellationToken.None);

            await workflowConsecution.GetResultAsync();
        }

        #region class GZipPayloadCodec
        class GZipPayloadCodec : IPayloadCodec
        {
            private const string MetadataKey = nameof(GZipPayloadCodec);

            public PayloadsCollection Decode(PayloadsCollection data)
            {
                MutablePayloadsCollection decompressedData = new();
                foreach (Payload p in data)
                {
                    decompressedData.Add(Decode(p));
                }

                return decompressedData;
            }

            public PayloadsCollection Encode(PayloadsCollection data)
            {
                MutablePayloadsCollection compressedData = new();
                foreach (Payload p in data)
                {
                    compressedData.Add(Encode(p));
                }

                return compressedData;
            }

            private Payload Decode(Payload data)
            {
                if (!data.Metadata.ContainsKey(MetadataKey))
                {
                    return data;
                }

                MutablePayload decoded = new();
                decoded.CopyMetadataFrom(data);
                decoded.RemoveMetadataEntry(MetadataKey);

                using GZipStream decodedStr = new(decoded.MutableData, CompressionMode.Decompress, leaveOpen: true);
                data.Data.CopyTo(decodedStr);

                return decoded;
            }

            private Payload Encode(Payload data)
            {
                if (data.Metadata.ContainsKey(MetadataKey))
                {
                    return data;
                }

                MutablePayload encoded = new();
                encoded.CopyMetadataFrom(data);
                encoded.SetMetadataEntry(MetadataKey);
                
                using GZipStream encodedStr = new(encoded.MutableData, CompressionLevel.Optimal, leaveOpen: true);
                data.Data.CopyTo(encodedStr);

                return encoded;
            }
        }
        #endregion class GZipPayloadCodec

        public static async Task UseInterceptorToLogClientCallsForAllWorkflows(string[] _)
        {
            TemporalServiceClientConfiguration serviceConfig = new()
            {
                Namespace = "namespace",
                TemporalServiceClientInterceptorFactory = (_, _, _, interceptors) =>
                            interceptors.Insert(0, new FileLoggerWorkflowClientInterceptor("SampleLog.txt")),
            };

            ITemporalServiceClient serviceClient = new TemporalServiceClient(serviceConfig);
            IWorkflowConsecution workflowConsecution = await serviceClient.GetWorkflowAsync("workflowId");

            await workflowConsecution.GetResultAsync();
        }

        public static async Task UseInterceptorToLogClientCallsForSpecificWorkflow(string[] _)
        {
            ITemporalServiceClient serviceClient = new TemporalServiceClient(new TemporalServiceClientConfiguration() { Namespace = "namespace" });

            WorkflowConsecutionClientConfiguration clientConfig = new()
            {
                TemporalServiceClientInterceptorFactory = (interceptors) =>
                {
                    interceptors.Insert(0, new FileLoggerWorkflowClientInterceptor("SampleLog.txt"));
                }
            };
            IWorkflowConsecution workflowConsecution = await serviceClient.GetWorkflowAsync(workflowTypeName: null,
                                                                                            workflowId: "xyz",
                                                                                            workflowConsecutionId: null,
                                                                                            clientConfig,
                                                                                            CancellationToken.None);

            await workflowConsecution.GetResultAsync();
        }

        #region class FileLoggerWorkflowClientInterceptor
        class FileLoggerWorkflowClientInterceptor : TemporalServiceClientInterceptorBase
        {
            private readonly string _fileName;

            public FileLoggerWorkflowClientInterceptor(string fileName)
            {
                _fileName = fileName;
            }

            protected override ITemporalServiceClientInterceptor.IWorkflowConsecutionInterceptor CreateConsecutionCallsInterceptor()
            {
                return new ConsecutionInterceptor();
            }

            protected override ITemporalServiceClientInterceptor.IWorkflowRunInterceptor CreatRunCallsInterceptor()
            {
                return new RunInterceptor();
            }

            public override Task<IWorkflowConsecution> OnGetWorkflowAsync(string workflowTypeName,
                                                                         string workflowId,
                                                                         string workflowConsecutionId,
                                                                         WorkflowConsecutionClientConfiguration clientConfig,
                                                                         CancellationToken cancelToken)
            {
                File.AppendAllText(_fileName,
                                   $"{nameof(OnGetWorkflowAsync)}({nameof(workflowTypeName)}:\"({workflowTypeName})\","
                                 + $" {nameof(workflowId)}:\"({workflowId})\","
                                 + $" {nameof(workflowConsecutionId)}:\"({workflowConsecutionId})\","
                                 + $"...)\n");

                return base.OnGetWorkflowAsync(workflowTypeName, workflowId, workflowConsecutionId, clientConfig, cancelToken);                
            }

            // Other OnXxx event handlers not implemented for brevity.

            class ConsecutionInterceptor : TemporalServiceClientInterceptorBase.WorkflowConsecutionInterceptorBase
            {
                public override Task<IWorkflowConsecutionResult<TResult>> OnGetResultAsync<TResult>(CancellationToken cancelToken)
                {
                    File.AppendAllText(((FileLoggerWorkflowClientInterceptor) Owner)._fileName,
                                       $"Consecution: {nameof(OnGetResultAsync)}()\n");

                    return base.OnGetResultAsync<TResult>(cancelToken);
                }

                // Other OnXxx event handlers not implemented for brevity.
            }

            class RunInterceptor : TemporalServiceClientInterceptorBase.WorkflowRunInterceptorBase
            {
                public override Task<WorkflowRunInfo> OnGetInfoAsync()
                {
                    File.AppendAllText(((FileLoggerWorkflowClientInterceptor)Owner)._fileName,
                                       $"Run: {nameof(OnGetInfoAsync)}()\n");

                    return base.OnGetInfoAsync();
                }

                // Other OnXxx event handlers not implemented for brevity.
            }
        }
        #endregion class FileLoggerWorkflowClientInterceptor
    }
}
