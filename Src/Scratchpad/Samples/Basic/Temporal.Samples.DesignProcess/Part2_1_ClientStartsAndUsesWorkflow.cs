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
        public static void Main(string[] _)
        {
        }

        public static async Task Minimal()
        {
            TemporalServiceClientConfiguration serviceConfig = new() { Namespace = "namespace" };
            ITemporalServiceClient serviceClient = await TemporalServiceClient.ConnectNewAsync(serviceConfig);

            IWorkflowChain workflowChain = await serviceClient.StartWorkflowAsync("workflowId", "workflowTypeName", "taskQueue");
            await workflowChain.GetResultAsync();

            Console.WriteLine($"Workflow finshed.");
        }

        public static async Task InvokeExamples()
        {
            TemporalServiceClientConfiguration serviceConfig = new() { Namespace = "namespace" };
            ITemporalServiceClient serviceClient = await TemporalServiceClient.ConnectNewAsync(serviceConfig);

            await GetResultOfWorkflowThatIsKnownToBeStarted(serviceClient);
            await QueryWorkflowThatIsKnownToBeStarted(serviceClient);
            await StartNewWorkflow1(serviceClient);
            // ...
        }

        public static async Task GetResultOfWorkflowThatIsKnownToBeStarted(ITemporalServiceClient serviceClient)
        {
            IWorkflowChain workflowChain = serviceClient.CreateUnboundWorkflowHandle("workflowId");

            // At this point, `workflowChain` will bind to the latest chain with `workflowId`, regardless of status:
            await workflowChain.GetResultAsync();

            Console.WriteLine("Workflow finished.");
        }

        internal record SomeDataValue() : IDataValue;

        public static async Task QueryWorkflowThatIsKnownToBeStarted(ITemporalServiceClient serviceClient)
        {
            IWorkflowChain workflowChain = serviceClient.CreateUnboundWorkflowHandle("workflowId");

            // This will query the currently latest chain (regardless of status), and then bind to the chain that ended up being queried.
            SomeDataValue val = await workflowChain.QueryAsync<SomeDataValue>("queryName");
            
            Console.WriteLine($"Query result: \"{val}\".");

            await workflowChain.GetResultAsync();
            Console.WriteLine("Workflow finished.");
        }

        public static async Task StartNewWorkflow1(ITemporalServiceClient serviceClient)
        {
            // This exemplifies the "largest" (most complete) Start Workflow overload:
            IWorkflowChain workflowChain = await serviceClient.StartWorkflowAsync(workflowId:       "SomeCustomerId",
                                                                                  workflowTypeName: "CustomerRegistrationProcess",
                                                                                  taskQueue:        "SomeTaskQueue",
                                                                                  inputArgs:        DataValue.Wrap(42),
                                                                                  workflowConfig:   new StartWorkflowChainConfiguration()
                                                                                                    {
                                                                                                        Identity = "Some Id"
                                                                                                    },
                                                                                  cancelToken: CancellationToken.None);

            await workflowChain.GetResultAsync();
            Console.WriteLine("Workflow finished.");
        }

        public static async Task StartNewWorkflow2(ITemporalServiceClient serviceClient)
        {
            // The previous example is equivalet to the following:
            IWorkflowChain workflowChain = serviceClient.CreateUnboundWorkflowHandle(workflowId: "SomeCustomerId");
            await workflowChain.StartAsync(workflowTypeName: "CustomerRegistrationProcess",
                                           taskQueue:        "SomeTaskQueue",
                                           inputArgs:        DataValue.Wrap(42),
                                           workflowConfig:   new StartWorkflowChainConfiguration()
                                                             {
                                                                 Identity = "Some Id"
                                                             },
                                           cancelToken:      CancellationToken.None);

            await workflowChain.GetResultAsync();
            Console.WriteLine("Workflow finished.");
        }

        public static async Task StartWorkflowOnlyIfNotAlreadyRunning(ITemporalServiceClient serviceClient)
        {
            // In this example, we need to connect to a workflow while ensuring it is running.
            // If the workflow is not already running, we will start it.
            // If we end up starting the workflow, we want to run some particular query.
            // Otherwise, we do not need to run any query, but we still want to bind the Chain Handle to the current chain.

            // ! In general, be aware that this code is not atomic in respect to concurrency with the remote workflow: !
            // A chain that was running at some time may finish by the time the client executes the next API.
            // However, a user familiar with the internal logic of a workflow may very well make certain safe assumptions
            // about cuncurrent scenarios.

            IWorkflowChain workflowChain = serviceClient.CreateUnboundWorkflowHandle("workflowId");

            if (await workflowChain.StartIfNotRunningAsync("workflowTypeName", "taskQueue"))
            {
                // Start has bound `workflowChain`, so we are guatanteed to query that chain that we just started:
                SomeDataValue val = await workflowChain.QueryAsync<SomeDataValue>("queryAfterStartName");
                Console.WriteLine($"Query result: \"{val}\".");
            }
            else
            {
                // Start returned False, implying that a chain with the specified workflow Id was already running. Bind to that chain.
                // Note that in general we have no guarantee that it is still running, but based on the knowledge of the overall
                // scenario, the developer may or may not assume that in practice.
                await workflowChain.EnsureBoundAsync();
            }

            // Work with the `workflowChain` instance...

            await workflowChain.GetResultAsync();
            Console.WriteLine("Workflow finished.");
        }

        public static async Task CheckIfParticularWorkflowExists(ITemporalServiceClient serviceClient)
        {
            const string WorkflowId = "Required Workflow Id";

            if ((await serviceClient.TryGetWorkflowAsync(WorkflowId)).IsSuccess(out IWorkflowChain workflowChain))
            {
                // `TryGetWorkflowAsync(..)` ensured that the chain exists and is bound,
                // so `GetWorkflowChainIdAsync()` will complete synchronously:

                Console.WriteLine($"Workflow with Workflow-Id \"{WorkflowId}\" exists"
                                + $" and its Workflow-Chain-Id is \"{ await workflowChain.GetWorkflowChainIdAsync()}\".");
            }
            else
            {
                Console.WriteLine($"Workflow with Workflow-Id \"{WorkflowId}\" does not exist.");
            }
        }

        public static async Task AvoidLongPolls(ITemporalServiceClient serviceClient)
        {
            IWorkflowChain workflowChain = serviceClient.CreateUnboundWorkflowHandle("workflowId");

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

        internal record ComputationResult(int Number) : IDataValue;

        public static async Task AccessResultOfWorkflow(ITemporalServiceClient serviceClient)
        {
            IWorkflowChain workflowChain = await serviceClient.StartWorkflowAsync("ComputeSomeNumber", "NumbersComputer", "TaskQueue");

            IWorkflowChainResult<ComputationResult> result = await workflowChain.GetResultAsync<ComputationResult>();
            Console.WriteLine($"The result of the workflow is {result.Value.Number}.");
        }

        public static async Task UseSignalsAndQueries(ITemporalServiceClient serviceClient)
        {
            // Start countdown timer to finish in 1 min:   
            IWorkflowChain workflowChain = await serviceClient.StartWorkflowAsync("TestTimerXyz",
                                                                                  "CountdownTimer",
                                                                                  "TaskQueue",
                                                                                  new TargetTimePayload(DateTime.UtcNow.AddMinutes(1)));

            // Do something else...
            await Task.Delay(TimeSpan.FromSeconds(30));

            // Push out target time by 30 secs:
            // Query the workflow, add a minute, signal workflow.
            TargetTimePayload prevTargetTime = await workflowChain.QueryAsync<TargetTimePayload>("GetCurrentTargetTimeUtc");
            TargetTimePayload newTargetTime = new(prevTargetTime.UtcDateTime.AddSeconds(30));
            await workflowChain.SignalAsync(RemoteApiNames.CountdownTimerWorkflow.Signals.UpdateTargetTime, newTargetTime);

            // Wait for the workflow to finish:
            CountdownResult result = (await workflowChain.GetResultAsync<CountdownResult>()).Value;
            Console.WriteLine($"The workflow {(result.IsTargetTimeReached ? "did" : "did not")} reach the target time.");
        }

        public static async Task CancelWorkflow(ITemporalServiceClient serviceClient)
        {
            // Get unbound handle to the latest workflow chain:
            IWorkflowChain latestChain = serviceClient.CreateUnboundWorkflowHandle("workflowId");

            // Request to cancel the latest chain:
            // The Task returned by this API is completed when the cancellation request call is completed,
            // i.e. the server persisted the request to cancel into the workflow history.
            // At that time the workflow implementation may not have yet processed the cancellation request.            
            await latestChain.RequestCancellationAsync();

            // After requesting the cancellation, `latestChain` will be bound to the chain that was actually addressed. 
            // The code below is safe in regard to interacting with the same chain.

            // We must await the completion of the workflow to learn whether the remote workflow actually
            // honored the cancellation request.
            // (note that if the workflow does not respect the cancellation, this may await indefinitely)
            IWorkflowChainResult result = await latestChain.GetResultAsync();
            Console.WriteLine("Workflow cancellation "
                            + (result.Status == WorkflowExecutionStatus.Canceled ? "was respected" : "was not respected")
                            + " by the workflow.");
        }

        public static async Task CancelWorkflow2(ITemporalServiceClient serviceClient)
        {
            // Get unbound handle to the latest workflow chain:
            IWorkflowChain latestChain = serviceClient.CreateUnboundWorkflowHandle("workflowId");

            // Request to cancel the latest chain:
            // The Task returned by this API is completed when the cancellation request call is completed,
            // i.e. the server persisted the request to cancel into the workflow history.
            // At that time the workflow implementation may not have yet processed the cancellation request.            
            await latestChain.RequestCancellationAsync();

            // After requesting the cancellation, `latestChain` will be bound to the chain that was actually addressed. 
            // The code below is safe in regard to interacting with the same chain.

            // If we want to know that the workflow reacted to the cancellation we must await its conclusion.            
            // (note that if the workflow does not respect the cancellation, this may await indefinitely)
            await latestChain.GetResultAsync();            
        }

        public static async Task CancelWorkflowAndWaitWithProgress(ITemporalServiceClient serviceClient)
        {
            TimeSpan progressUpdatePeriod = TimeSpan.FromSeconds(10);

            IWorkflowChain workflow = serviceClient.CreateUnboundWorkflowHandle("workflowId");

            // Request cancellation and bind the handle:
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

        public static async Task AccessWorkflowResultValue(ITemporalServiceClient serviceClient)
        {
            // Get the workflow handle:
            IWorkflowChain workflow = serviceClient.CreateUnboundWorkflowHandle("workflowId");

            // Get bind the handle and the workflow result:
            ComputationResult resultValue = (await workflow.GetResultAsync<ComputationResult>()).Value;

            // Print result:
            Console.WriteLine($"Workflow completed. Result value: \"{resultValue}\".");
        }

        public static async Task AccessWorkflowResultValue2(ITemporalServiceClient serviceClient)
        {
            // Get the workflow handle:
            IWorkflowChain workflow = serviceClient.CreateUnboundWorkflowHandle("workflowId");

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


        public static async Task UsePayloadCodecToCompressPayloadsForAllWorkflows()
        {
            TemporalServiceClientConfiguration serviceConfig = new()
            {
                DataConverterFactory = (_, _, _) => new DefaultDataConverter(new[] { new GZipPayloadCodec() }),
                Namespace = "namespace",
            };

            ITemporalServiceClient serviceClient = await TemporalServiceClient.ConnectNewAsync(serviceConfig);
            IWorkflowChain workflowChain = await serviceClient.StartWorkflowAsync("workflowId", "workflowTypeName", "taskQueue");

            await workflowChain.GetResultAsync();
        }

        #region class GZipPayloadCodec
        internal class GZipPayloadCodec : IPayloadCodec
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

        public static async Task UseInterceptorToLogClientCallsForAllWorkflows()
        {
            TemporalServiceClientConfiguration serviceConfig = new()
            {
                Namespace = "namespace",
                TemporalServiceClientInterceptorFactory = (_, _, _, interceptors) =>
                            interceptors.Insert(0, new FileLoggerWorkflowClientInterceptor("SampleLog.txt")),
            };

            ITemporalServiceClient serviceClient = await TemporalServiceClient.ConnectNewAsync(serviceConfig);
            IWorkflowChain workflowChain = serviceClient.CreateUnboundWorkflowHandle("workflowId");

            await workflowChain.GetResultAsync();
        }

        #region class FileLoggerWorkflowClientInterceptor
        internal class FileLoggerWorkflowClientInterceptor : TemporalServiceClientInterceptorBase
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
