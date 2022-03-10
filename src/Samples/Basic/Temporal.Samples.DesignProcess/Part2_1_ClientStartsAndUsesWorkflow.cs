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
            WorkflowIsAlreadyRunning(args).GetAwaiter().GetResult();
            WorkflowMayAlreadyBeRunning(args).GetAwaiter().GetResult();
            AvoidLongPolls(args).GetAwaiter().GetResult();
            AccessResultOfWorkflow(args).GetAwaiter().GetResult();
            UseSignalsAndQueries(args).GetAwaiter().GetResult();
            CancelWorkflow(args).GetAwaiter().GetResult();
            UsePayloadCodecToCompressPayloadsForAllWorkflows(args).GetAwaiter().GetResult();
        }

        public static async Task Minimal(string[] _)
        {
            TemporalServiceClientConfiguration serviceConfig = new() { Namespace = "namespace" };
            ITemporalServiceClient serviceClient = await TemporalServiceClient.CreateAndInitializeAsync(serviceConfig);

            IWorkflowChain workflowChain = await serviceClient.StartWorkflowAsync("workflowTypeName", "workflowId", "taskQueue");

            IWorkflowChainResult result = await workflowChain.GetResultAsync();
            Console.WriteLine($"Final state: {result.Status}.");
        }

        public static async Task WorkflowIsAlreadyRunning(string[] _)
        {
            TemporalServiceClientConfiguration serviceConfig = new() { Namespace = "namespace" };
            ITemporalServiceClient serviceClient = await TemporalServiceClient.CreateAndInitializeAsync(serviceConfig);

            IWorkflowChain workflowChain = await serviceClient.GetWorkflowAsync("workflowId");

            await workflowChain.GetResultAsync();
            Console.WriteLine("Workflow completed.");
        }

        public static async Task WorkflowMayAlreadyBeRunning(string[] _)
        {
            TemporalServiceClientConfiguration serviceConfig = new() { Namespace = "namespace" };
            ITemporalServiceClient serviceClient = await TemporalServiceClient.CreateAndInitializeAsync(serviceConfig);

            IWorkflowChain workflowChain = await serviceClient.GetOrStartWorkflowAsync("workflowTypeName", "workflowId", "taskQueue", CancellationToken.None);

            await workflowChain.GetResultAsync();
            Console.WriteLine("Workflow completed.");
        }

        public static async Task AvoidLongPolls(string[] _)
        {
            TemporalServiceClientConfiguration serviceConfig = new() { Namespace = "namespace" };
            ITemporalServiceClient serviceClient = await TemporalServiceClient.CreateAndInitializeAsync(serviceConfig);

            IWorkflowChain workflowChain = await serviceClient.GetWorkflowAsync("workflowId");

            if ((await workflowChain.TryGetResultIfAvailableAync()).IsSuccess(out IWorkflowChainResult result))
            {                                
                Console.WriteLine($"Workflow chain completed with status: {result.Status}.");
            }
            
            Console.WriteLine($"Workflow did not yet complete. Doing some other work...");
            await Task.Delay(TimeSpan.FromSeconds(60));

            if ((await workflowChain.TryGetResultIfAvailableAync()).IsSuccess(out result))
            {
                Console.WriteLine($"Workflow chain completed with status: {result.Status}.");
            }

            Console.WriteLine($"Workflow still did not complete. Good bye.");
        }

        record ComputationResult(int Number) : IDataValue;

        public static async Task AccessResultOfWorkflow(string[] _)
        {
            TemporalServiceClientConfiguration serviceConfig = new() { Namespace = "namespace" };
            ITemporalServiceClient serviceClient = await TemporalServiceClient.CreateAndInitializeAsync(serviceConfig);

            IWorkflowChain workflowChain = await serviceClient.StartWorkflowAsync("NumbersComputer", "ComputeSomeNumber", "TaskQueue");

            IWorkflowChainResult<ComputationResult> result = await workflowChain.GetResultAsync<ComputationResult>();
            Console.WriteLine($"The result of the workflow is {result.Value.Number}.");
        }

        public static async Task UseSignalsAndQueries(string[] _)
        {
            TemporalServiceClientConfiguration serviceConfig = new() { Namespace = "namespace" };
            ITemporalServiceClient serviceClient = await TemporalServiceClient.CreateAndInitializeAsync(serviceConfig);

            // Start countdown timer to finish in 1 min:   
            IWorkflowChain workflowChain = await serviceClient.StartWorkflowAsync("CountdownTimer",
                                                                                  "TestTimerXyz",
                                                                                  "TaskQueue",
                                                                                  new TargetTimePayload(DateTime.UtcNow.AddMinutes(1)));

            // Do something else...
            await Task.Delay(TimeSpan.FromSeconds(30));

            // Push out target time by 30 secs:
            // Query the workflow, add a minute, signal workflow.
            TargetTimePayload prevTargetTime = (await workflowChain.QueryAsync<TargetTimePayload>("GetCurrentTargetTimeUtc")).Value;
            TargetTimePayload newTargetTime = new(prevTargetTime.UtcDateTime.AddSeconds(30));
            await workflowChain.SignalAsync(RemoteApiNames.CountdownTimerWorkflow.Signals.UpdateTargetTime, newTargetTime);

            // Wait for the workflow to finish:
            CountdownResult result = (await workflowChain.GetResultAsync<CountdownResult>()).Value;
            Console.WriteLine($"The workflow {(result.IsTargetTimeReached ? "did" : "did not")} reach the target time.");
        }

        public static async Task CancelWorkflow(string[] _)
        {
            TemporalServiceClientConfiguration serviceConfig = new() { Namespace = "namespace" };
            ITemporalServiceClient serviceClient = await TemporalServiceClient.CreateAndInitializeAsync(serviceConfig);

            // Get latest workflow with the specified Id or throw if no workflows with the specified Id can be found:
            IWorkflowChain latestChain = await serviceClient.GetWorkflowAsync("workflowId");

            // The Task returned by this API is completed when the cancellation request call is completed,
            // i.e. the server persisted the request to cancel into the workflow history.
            // At that time the workflow implementation may not have yet processed the cancellation request.
            // This API will throw if latestChain is no longer running which may happen concurrently
            // after the above check.
            await latestChain.RequestCancellationAsync();

            // We must await the completion of the workflow to learn whether the remote workflow actually
            // honored the cancellation request.
            // (note that if the workflow does not respect the cancellation, this may await indefinitely)
            IWorkflowChainResult result = await latestChain.GetResultAsync();
            Console.WriteLine("Workflow cancellation "
                            + (result.Status == WorkflowExecutionStatus.Canceled ? "was respected" : "was not respected")
                            + " by the workflow.");
        }

        public static async Task CancelWorkflow2(string[] _)
        {
            TemporalServiceClientConfiguration serviceConfig = new() { Namespace = "namespace" };
            ITemporalServiceClient serviceClient = await TemporalServiceClient.CreateAndInitializeAsync(serviceConfig);

            // Get latest workflow with the specified Id or throw if no workflows with the specified Id can be found:
            IWorkflowChain latestChain = await serviceClient.GetWorkflowAsync("workflowId");
            
            // The Task returned by this API is completed when the cancellation request call is completed,
            // i.e. the server persisted the request to cancel into the workflow history.
            // At that time the workflow implementation may not have yet processed the cancellation request.
            // This API will throw if latestChain is no longer running which may happen concurrently
            // after the above check.
            await latestChain.RequestCancellationAsync();

            // If we want to know that the workflow reacted to the cancellation we must await its conclusion.            
            // (note that if the workflow does not respect the cancellation, this may await indefinitely)
            await latestChain.GetResultAsync();            
        }

        public static async Task CancelWorkflowAndWaitWithProgress()
        {
            TimeSpan progressUpdatePeriod = TimeSpan.FromSeconds(10);

            TemporalServiceClientConfiguration serviceConfig = new() { Namespace = "namespace" };
            ITemporalServiceClient serviceClient = await TemporalServiceClient.CreateAndInitializeAsync(serviceConfig);
           
            IWorkflowChain workflow = await serviceClient.GetWorkflowAsync("workflowId");

            // Request cancellation:
            await workflow.RequestCancellationAsync();
            DateTime cancellationTime = DateTime.Now;

            // This task represents the conclusion of the workflow with any status:
            Task<IWorkflowChainResult> workflowConclusion = workflow.GetResultAsync();

            // This task represents the conclusion of the display update period:
            Task displayDelayConclusion = Task.Delay(progressUpdatePeriod);

            // Await until either the workflow or the waiting period finishes:
            await Task.WhenAny(workflowConclusion, displayDelayConclusion);

            // If workflow is still running, display progress and keep waiting:
            while(! workflowConclusion.IsCompleted)
            {
                Console.WriteLine($"Still waiting for the workflow to react to the cancellation request."
                                + $" Time elapsed: {DateTime.Now - cancellationTime}.");

                displayDelayConclusion = Task.Delay(progressUpdatePeriod);
                await Task.WhenAny(workflowConclusion, displayDelayConclusion);
            }

            // Get the result handle and display the final status:
            IWorkflowChainResult workflowResult = workflowConclusion.Result;
            Console.WriteLine($"Workflow finished. Terminal status: {workflowResult.Status}.");
        }

        public static async Task AccessWorkflowResultValue()
        {
            // Get the workflow:
            TemporalServiceClientConfiguration serviceConfig = new() { Namespace = "namespace" };
            ITemporalServiceClient serviceClient = await TemporalServiceClient.CreateAndInitializeAsync(serviceConfig);
            IWorkflowChain workflow = await serviceClient.GetWorkflowAsync("workflowId");
            
            // Get the workflow result:
            ComputationResult resultValue = (await workflow.GetResultAsync<ComputationResult>()).Value;

            // Print result:
            Console.WriteLine($"Workflow completed. Result value: \"{resultValue}\".");
        }

        public static async Task AccessWorkflowResultValue2()
        {
            // Get the workflow:
            TemporalServiceClientConfiguration serviceConfig = new() { Namespace = "namespace" };
            ITemporalServiceClient serviceClient = await TemporalServiceClient.CreateAndInitializeAsync(serviceConfig);           
            IWorkflowChain workflow = await serviceClient.GetWorkflowAsync("workflowId");

            // Declare variables used below:
            Task<IWorkflowChainResult<ComputationResult>> workflowConclusion;
            IWorkflowChainResult<ComputationResult> workflowResult;
            ComputationResult resultValue;

            // Expand the ONE LINE in the previous sample into small steps and add try-catch clauses that clarify
            // what exceptions may or may not occur at each step:
            
            try
            {
                workflowConclusion = workflow.GetResultAsync<ComputationResult>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while initiating the long poll to wait for the conclusion of the workflow: {ex}");
                return;
            }

            try
            {
                workflowResult = await workflowConclusion;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Network or Temporal server error while waiting for the workflow to run to conclusion: {ex}");
                return;
            }

            try
            {
                resultValue = workflowResult.Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting workflow result value"
                                + $" (workflow run to conclusion, but it did not complete successfully): {ex}");
                return;
            }
           
            Console.WriteLine($"Workflow completed. Result value: \"{resultValue}\".");
        }


        public static async Task UsePayloadCodecToCompressPayloadsForAllWorkflows(string[] _)
        {
            TemporalServiceClientConfiguration serviceConfig = new()
            {
                DataConverterFactory = (_, _, _) => new DefaultDataConverter(new[] { new GZipPayloadCodec() }),
                Namespace = "namespace",
            };

            ITemporalServiceClient serviceClient = new TemporalServiceClient(serviceConfig);
            IWorkflowChain workflowChain = await serviceClient.StartWorkflowAsync("workflowTypeName", "workflowId", "taskQueue");

            await workflowChain.GetResultAsync();
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
            IWorkflowChain workflowChain = await serviceClient.GetWorkflowAsync("workflowId");

            await workflowChain.GetResultAsync();
        }

        #region class FileLoggerWorkflowClientInterceptor
        class FileLoggerWorkflowClientInterceptor : TemporalServiceClientInterceptorBase
        {
            private readonly string _fileName;

            public FileLoggerWorkflowClientInterceptor(string fileName)
            {
                _fileName = fileName;
            }

            public override Task<IWorkflowChain> OnServiceClient_GetWorkflowAsync(string workflowId,
                                                                                  string workflowChainId,
                                                                                  CancellationToken cancelToken)
            {
                File.AppendAllText(_fileName,
                                   $"{nameof(OnServiceClient_GetWorkflowAsync)}({nameof(workflowId)}:\"({workflowId})\","
                                 + $" {nameof(workflowChainId)}:\"({workflowChainId})\","
                                 + $"...)\n");

                return base.OnServiceClient_GetWorkflowAsync(workflowId, workflowChainId, cancelToken);                
            }

            // Other OnServiceClient_Xxx event handlers not implemented for brevity.

            
            public override Task<IWorkflowChainResult<TResult>> OnChain_GetResultAsync<TResult>(CancellationToken cancelToken)
            {
                File.AppendAllText(_fileName,
                                   $"{nameof(OnChain_GetResultAsync)}(..)\n");

                return base.OnChain_GetResultAsync<TResult>(cancelToken);
            }

            // Other OnChain_Xxx event handlers not implemented for brevity.

            public override Task<WorkflowRunInfo> OnRun_GetInfoAsync()
            {
                File.AppendAllText(_fileName,
                                   $"{nameof(OnRun_GetInfoAsync)}()\n");

                return base.OnRun_GetInfoAsync();
            }

            // Other OnRun_Xxx event handlers not implemented for brevity.
        }
        #endregion class FileLoggerWorkflowClientInterceptor
    }
}
