using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Temporal.Common.DataModel;
using Temporal.Common.Exceptions;
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
            TemporalClientConfiguration serviceConfig = new() { Namespace = "namespace" };
            ITemporalClient client = await TemporalClient.ConnectAsync(serviceConfig);

            IWorkflowChain workflowChain = await client.StartWorkflowAsync("workflowId", "workflowTypeName", "taskQueue");
            await workflowChain.GetResultAsync();

            Console.WriteLine($"Workflow finshed.");
        }

        public static async Task InvokeExamples()
        {
            TemporalClientConfiguration serviceConfig = new() { Namespace = "namespace" };
            ITemporalClient client = await TemporalClient.ConnectAsync(serviceConfig);

            await GetResultOfWorkflowThatIsKnownToBeStarted(client);
            await QueryWorkflowThatIsKnownToBeStarted(client);
            await StartNewWorkflow1(client);
            // ...
        }

        public static async Task GetResultOfWorkflowThatIsKnownToBeStarted(ITemporalClient client)
        {
            IWorkflowChain workflowChain = client.CreateWorkflowHandle("workflowId");

            // At this point, `workflowChain` will bind to the latest chain with `workflowId`, regardless of status:
            await workflowChain.GetResultAsync();

            Console.WriteLine("Workflow finished.");
        }

        internal record SomeDataValue() : IDataValue;

        public static async Task QueryWorkflowThatIsKnownToBeStarted(ITemporalClient client)
        {
            IWorkflowChain workflowChain = client.CreateWorkflowHandle("workflowId");

            // This will query the currently latest chain (regardless of status), and then bind to the chain that ended up being queried.
            SomeDataValue val = await workflowChain.QueryAsync<SomeDataValue>("queryName");
            
            Console.WriteLine($"Query result: \"{val}\".");

            await workflowChain.GetResultAsync();
            Console.WriteLine("Workflow finished.");
        }

        public static async Task StartNewWorkflow1(ITemporalClient client)
        {
            // This exemplifies the "largest" (most complete) Start Workflow overload:
            IWorkflowChain workflowChain = await client.StartWorkflowAsync(workflowId:       "SomeCustomerId",
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

        public static async Task StartNewWorkflow2(ITemporalClient client)
        {
            // The previous example is equivalet to the following:
            IWorkflowChain workflowChain = client.CreateWorkflowHandle(workflowId: "SomeCustomerId");
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

        public static async Task StartWorkflowOnlyIfNotAlreadyRunning(ITemporalClient client)
        {
            // In this example, we need to connect to a workflow while ensuring it is running.
            // If the workflow is not already running, we will start it.
            // If we end up starting the workflow, we want to run some particular query.
            // Otherwise, we do not need to run any query, but we still want to bind the Chain Handle to the current chain,
            // becasue we need its id for processing.

            // ! In general, be aware that this code is not atomic in respect to concurrency with the remote workflow: !
            // A chain that was running at some time may finish by the time the client executes the next API.
            // However, a user familiar with the internal logic of a workflow may very well make certain safe assumptions
            // about cuncurrent scenarios.

            IWorkflowChain workflowChain = client.CreateWorkflowHandle("workflowId");

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

            string workflowChainId = workflowChain.WorkflowChainId;

            Console.WriteLine($"Chain with ID \"{workflowChainId}\" will be processed.");
            // Work with the `workflowChain` instance...

            await workflowChain.GetResultAsync();
            Console.WriteLine("Workflow finished.");
        }

        public static async Task CheckIfParticularWorkflowExists(ITemporalClient client)
        {
            const string WorkflowId = "Required Workflow Id";

            if (await client.CreateWorkflowHandle(WorkflowId).CheckExistsAsync())
            {
                Console.WriteLine($"Workflow with Workflow-Id \"{WorkflowId}\" exists.");
            }
            else
            {
                Console.WriteLine($"Workflow with Workflow-Id \"{WorkflowId}\" does not exist.");
            }
        }

        public static async Task AvoidLongPolls(ITemporalClient client)
        {
            IWorkflowChain workflowChain = client.CreateWorkflowHandle("workflowId");            

            if ((await workflowChain.TryGetFinalRunAsync()).IsSuccess(out IWorkflowRun finalRun))
            {
                WorkflowExecutionStatus status = await finalRun.GetStatusAsync();
                if (status != WorkflowExecutionStatus.Running)
                {
                    Console.WriteLine($"Workflow chain completed with status: {status}.");
                    return;
                }                
            }

            Console.WriteLine($"Workflow did not yet complete. Doing some other work...");
            await Task.Delay(TimeSpan.FromSeconds(60));

            if ((await workflowChain.TryGetFinalRunAsync()).IsSuccess(out finalRun))
            {
                WorkflowExecutionStatus status = await finalRun.GetStatusAsync();
                if (status != WorkflowExecutionStatus.Running)
                {
                    Console.WriteLine($"Workflow chain completed with status: {status}.");
                    return;
                }
            }

            Console.WriteLine($"Workflow still did not complete. Good bye.");
        }

        internal record ComputationResult(int Number) : IDataValue;

        public static async Task AccessResultOfWorkflow(ITemporalClient client)
        {
            IWorkflowChain workflowChain = await client.StartWorkflowAsync("ComputeSomeNumber", "NumbersComputer", "TaskQueue");

            ComputationResult result = await workflowChain.GetResultAsync<ComputationResult>();
            Console.WriteLine($"The result of the workflow is {result.Number}.");
        }

        public static async Task UseSignalsAndQueries(ITemporalClient client)
        {
            // Start countdown timer to finish in 1 min:   
            IWorkflowChain workflowChain = await client.StartWorkflowAsync("TestTimerXyz",
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
            CountdownResult result = await workflowChain.GetResultAsync<CountdownResult>();
            Console.WriteLine($"The workflow {(result.IsTargetTimeReached ? "did" : "did not")} reach the target time.");
        }

        public static async Task CancelWorkflow(ITemporalClient client)
        {
            // Get unbound handle to the latest workflow chain:
            IWorkflowChain latestChain = client.CreateWorkflowHandle("workflowId");

            // Request to cancel the latest chain:
            // The Task returned by this API is completed when the cancellation request call is completed,
            // i.e. the server persisted the request to cancel into the workflow history.
            // At that time the workflow implementation may not have yet processed the cancellation request.            
            await latestChain.RequestCancellationAsync();

            // After requesting the cancellation, `latestChain` will be bound to the chain that was actually addressed. 
            // The code below is safe in regard to interacting with the same chain.

            // We must await the completion of the workflow to learn whether the remote workflow actually
            // honored the cancellation request.
            // (!note that if the workflow does not respect the cancellation, this may await indefinitely!)
            try
            {
                await latestChain.GetResultAsync();
            }
            catch (RemoteTemporalException rtEx) when (rtEx.InnerException is CancellationException)
            {
                Console.WriteLine("Workflow cancellation was respected by the workflow.");
                return;
            }
            catch (RemoteTemporalException rtEx)
            {
                Console.WriteLine($"Workflow cancellation was NOT respected by the workflow,"
                                + $" and the workflow did not complete successfully ({rtEx.InnerException}).");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Not clear whether the Workflow cancellation was respected because of en error ({ex}).");
                return;
            }

            Console.WriteLine($"Not clear whether the Workflow cancellation was NOT respected and the workflow completed.");
        }

        public static async Task CancelWorkflow2(ITemporalClient client)
        {
            // The above example uses Exceptions to control the expected flow of the execution logic.
            // TO avoid that use the `AwaitConclusionAync(..)` API.

            IWorkflowChain latestChain = client.CreateWorkflowHandle("workflowId");

            await latestChain.RequestCancellationAsync();

            try
            {
                IWorkflowChainResult conclusion = await latestChain.AwaitConclusionAync();

                Console.WriteLine("Workflow cancellation "
                                + (conclusion.Status == WorkflowExecutionStatus.Canceled ? "was respected" : "was not respected")
                                + " by the workflow.");                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Not clear whether the Workflow cancellation was respected because of en error ({ex}).");
            }
        }

        public static async Task CancelWorkflowAndWaitWithProgress1(ITemporalClient client)
        {
            // Awating the conclusion of a workflow using `GetResultAsync(..)` and exceptions to control execution flow:

            TimeSpan progressUpdatePeriod = TimeSpan.FromSeconds(10);

            IWorkflowChain workflow = client.CreateWorkflowHandle("workflowId");

            // Request cancellation and bind the handle:
            await workflow.RequestCancellationAsync();
            DateTime cancellationTime = DateTime.Now;

            bool cancelApplied = false;

            // This task represents the successful completion of the workflow:
            Task workflowCompletion = workflow.GetResultAsync();

            // This task represents the conclusion of the display update period:
            Task displayDelayConclusion = Task.Delay(progressUpdatePeriod);

            try
            {
                // Await until either the workflow or the waiting period finishes:
                await Task.WhenAny(workflowCompletion, displayDelayConclusion);
            }
            catch(RemoteTemporalException rtEx) when(rtEx.InnerException is CancellationException)
            {
                cancelApplied = true;
            }

            // If workflow is still running, display progress and keep waiting:
            while (! cancelApplied)
            {
                Console.WriteLine($"Still waiting for the workflow to react to the cancellation request."
                                + $" Time elapsed: {DateTime.Now - cancellationTime}.");

                displayDelayConclusion = Task.Delay(progressUpdatePeriod);

                try
                {
                    // Await until either the workflow or the waiting period finishes:
                    await Task.WhenAny(workflowCompletion, displayDelayConclusion);
                }
                catch (RemoteTemporalException rtEx) when (rtEx.InnerException is CancellationException)
                {
                    cancelApplied = true;
                }
            }

            // Get the result handle and display the final status:            
            Console.WriteLine($"Workflow finished. Terminal status: {await workflow.GetStatusAsync()}.");
        }

        public static async Task CancelWorkflowAndWaitWithProgress2(ITemporalClient client)
        {
            // An alternative approach uses `AwaitConclusionAync(..)` and avoids using exceptions to control execution flow:

            TimeSpan progressUpdatePeriod = TimeSpan.FromSeconds(10);

            IWorkflowChain workflow = client.CreateWorkflowHandle("workflowId");

            // Request cancellation and bind the handle:
            await workflow.RequestCancellationAsync();
            DateTime cancellationTime = DateTime.Now;

            // This task represents the conclusion of the workflow with any status:
            Task<IWorkflowChainResult> workflowConclusion = workflow.AwaitConclusionAync();

            // This task represents the conclusion of the display update period:
            Task displayDelayConclusion = Task.Delay(progressUpdatePeriod);

            // Await until either the workflow or the waiting period finishes:
            await Task.WhenAny(workflowConclusion, displayDelayConclusion);

            // If workflow is still running, display progress and keep waiting:
            while (!workflowConclusion.IsCompleted)
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

        public static async Task AccessWorkflowResultValue1(ITemporalClient client)
        {
            // Get the workflow handle:
            IWorkflowChain workflow = client.CreateWorkflowHandle("workflowId");

            // Get bind the handle and the workflow result:
            int resultValue = await workflow.GetResultAsync<int>();

            // Print result:
            Console.WriteLine($"Workflow completed. Result value: \"{resultValue}\".");
        }

        public static async Task AccessWorkflowResultValue1_Expanded(ITemporalClient client)
        {
            // Get the workflow handle:
            IWorkflowChain workflow = client.CreateWorkflowHandle("workflowId");

            // Declare variables used below:
            Task<int> workflowCompletion;
            int resultValue;

            // Expand the ONE LINE in the previous sample into small steps and add try-catch clauses that clarify
            // what exceptions may or may not occur at each step:

            try
            {
                workflowCompletion = workflow.GetResultAsync<int>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while initiating the long poll to wait for the completion of the workflow: {ex}");
                return;
            }

            try
            {
                resultValue = await workflowCompletion;
            }
            catch (RemoteTemporalException rtEx)
            {
                Console.WriteLine($"The workflow run to conclusion, but the final status was not successful ({rtEx}).");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Some error prevented from obtaining the result/terminal-status of the workflow."
                                + $" It is not known whether the workflow actually finished or not ({ex}).");
                return;
            }

            // Print result:
            Console.WriteLine($"Workflow completed. Result value: \"{resultValue}\".");
        }

        public static async Task AccessWorkflowResultValue2(ITemporalClient client)
        {
            // Get the workflow handle:
            IWorkflowChain workflow = client.CreateWorkflowHandle("workflowId");

            // Get bind the handle and the workflow result:
            int resultValue = (await workflow.AwaitConclusionAync()).GetValue<int>();

            // Print result:
            Console.WriteLine($"Workflow completed. Result value: \"{resultValue}\".");
        }

        public static async Task AccessWorkflowResultValue2_Expanded(ITemporalClient client)
        {
            // Get the workflow handle:
            IWorkflowChain workflow = client.CreateWorkflowHandle("workflowId");

            // Declare variables used below:
            Task<IWorkflowChainResult> workflowConclusion;
            IWorkflowChainResult workflowResult;
            int resultValue;

            // Expand the ONE LINE in the previous sample into small steps and add try-catch clauses that clarify
            // what exceptions may or may not occur at each step:
            
            try
            {
                workflowConclusion = workflow.AwaitConclusionAync();
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
                resultValue = workflowResult.GetValue<int>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"The workflow run to conclusion, but the final status was not successful ({ex}).");
                return;
            }
           
            Console.WriteLine($"Workflow completed. Result value: \"{resultValue}\".");
        }


        public static async Task UsePayloadCodecToCompressPayloadsForAllWorkflows()
        {
            TemporalClientConfiguration serviceConfig = new()
            {
                DataConverterFactory = (_, _, _) => new DefaultDataConverter(new[] { new GZipPayloadCodec() }),
                Namespace = "namespace",
            };

            ITemporalClient client = await TemporalClient.ConnectAsync(serviceConfig);
            IWorkflowChain workflowChain = await client.StartWorkflowAsync("workflowId", "workflowTypeName", "taskQueue");

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
            TemporalClientConfiguration serviceConfig = new()
            {
                Namespace = "namespace",
                TemporalClientInterceptorFactory = (_, _, _, interceptors) =>
                            interceptors.Insert(0, new FileLoggerWorkflowClientInterceptor("SampleLog.txt")),
            };

            ITemporalClient client = await TemporalClient.ConnectAsync(serviceConfig);
            IWorkflowChain workflowChain = client.CreateWorkflowHandle("workflowId");

            await workflowChain.GetResultAsync();
        }

        #region class FileLoggerWorkflowClientInterceptor
        internal class FileLoggerWorkflowClientInterceptor : TemporalClientInterceptorBase
        {
            private readonly string _fileName;

            public FileLoggerWorkflowClientInterceptor(string fileName)
            {
                _fileName = fileName;
            }

            public override Task<IWorkflowChain> OnClient_GetWorkflowAsync(string workflowId,
                                                                                  string workflowChainId,
                                                                                  CancellationToken cancelToken)
            {
                File.AppendAllText(_fileName,
                                   $"{nameof(OnClient_GetWorkflowAsync)}({nameof(workflowId)}:\"({workflowId})\","
                                 + $" {nameof(workflowChainId)}:\"({workflowChainId})\","
                                 + $"...)\n");

                return base.OnClient_GetWorkflowAsync(workflowId, workflowChainId, cancelToken);                
            }

            // Other OnClient_Xxx event handlers not implemented for brevity.


            public override Task<TResult> OnChain_GetResultAsync<TResult>(CancellationToken cancelToken)
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
