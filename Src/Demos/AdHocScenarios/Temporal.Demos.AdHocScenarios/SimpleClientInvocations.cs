using System;
using System.Threading.Tasks;
using Temporal.Util;
using Temporal.Api.Enums.V1;
using Temporal.Common;
using Temporal.WorkflowClient;
using Temporal.WorkflowClient.Errors;
using Temporal.WorkflowClient.Interceptors;
using System.Threading;

namespace Temporal.Demos.AdHocScenarios
{
    internal class SimpleClientInvocations
    {
        public void Run()
        {
            Console.WriteLine();

            RunAsync().GetAwaiter().GetResult();

            Console.WriteLine();
            Console.WriteLine("Done. Press Enter.");
            Console.ReadLine();
        }

        public async Task RunAsync()
        {
            Console.WriteLine("Creating a client...");
            ITemporalClient client = await TemporalClient.ConnectAsync(TemporalClientConfiguration.ForLocalHost());

            string demoWfId = "Demo Workflow XYZ / " + Format.AsReadablePreciseLocal(DateTimeOffset.Now);

            Console.WriteLine("Starting a workflow...");
            IWorkflowHandle workflow = await client.StartWorkflowAsync(demoWfId,
                                                                      "DemoWorkflowTypeName",
                                                                      "DemoTaskQueue");
            Console.WriteLine("Started. Info:");
            Console.WriteLine($"    Namespace:       {workflow.Namespace}");
            Console.WriteLine($"    WorkflowId:      {workflow.WorkflowId}");
            Console.WriteLine($"    IsBound:         {workflow.IsBound}");
            Console.WriteLine($"    WorkflowChainId: {workflow.WorkflowChainId}");

            Console.WriteLine();
            Console.WriteLine("Attempting to start a workflow with the same WorkflowId...");
            Console.WriteLine();

            try
            {
                await client.StartWorkflowAsync(demoWfId, "DemoWorkflowTypeName", "DemoTaskQueue");

                throw new Exception("ERROR. We should never get here, because the above code is expected to throw.");
            }
            catch (WorkflowAlreadyExistsException waeEx)
            {
                Console.WriteLine("Received expected exception.");
                Console.WriteLine(waeEx.TypeAndMessage());

                Exception innerEx = waeEx.InnerException;
                while (innerEx != null)
                {
                    Console.WriteLine("\n  Inner --> " + innerEx.TypeAndMessage());
                    innerEx = innerEx.InnerException;
                }
            }

            Console.WriteLine();
            Console.WriteLine("Creating a handle to the existing workflow...");
            Console.WriteLine();

            IWorkflowHandle workflow2 = client.CreateWorkflowHandle(demoWfId);

            Console.WriteLine("Created. Info:");
            Console.WriteLine($"    Namespace:       {workflow2.Namespace}");
            Console.WriteLine($"    WorkflowId:      {workflow2.WorkflowId}");
            Console.WriteLine($"    IsBound:         {workflow2.IsBound}");

            try
            {
                Console.WriteLine($"    WorkflowChainId: {workflow2.WorkflowChainId}");

                throw new Exception("ERROR. We should never get here, because the above code is expected to throw.");
            }
            catch (InvalidOperationException invOpEx)
            {
                Console.WriteLine($"    Expected exception while getting {nameof(workflow2.WorkflowChainId)}:");
                Console.WriteLine($"    --> {invOpEx.TypeAndMessage()}");

                Exception innerEx = invOpEx.InnerException;
                while (innerEx != null)
                {
                    Console.WriteLine("\n      Inner --> " + invOpEx.TypeAndMessage());
                    innerEx = innerEx.InnerException;
                }
            }

            Console.WriteLine();
            Console.WriteLine("Obtaining Workflow Type Name...");
            Console.WriteLine();

            string workflowTypeName = await workflow2.GetWorkflowTypeNameAsync();

            Console.WriteLine($"Obtained. {nameof(workflowTypeName)}={workflowTypeName.QuoteOrNull()}.");
            Console.WriteLine("Updated handle info:");
            Console.WriteLine($"    IsBound:         {workflow2.IsBound}");
            Console.WriteLine($"    WorkflowChainId: {workflow2.WorkflowChainId}");

            Console.WriteLine();
            Console.WriteLine("Sending signal to a workflow...");
            Console.WriteLine();

            await workflow.SignalAsync("Some-Signal-01", "Some-Signal-Argument");

            Console.WriteLine("Signal sent. Look for it in the workflow history.");

            Console.WriteLine();
            Console.WriteLine("Sending query to a workflow...");

            try
            {
                TimeSpan delayQueryCancel = TimeSpan.FromSeconds(2);
                Console.WriteLine($"The workflow was not designed for queries,"
                                + $" so we will cancel the wait for query completion after '{delayQueryCancel}'.");
                Console.WriteLine();

                using CancellationTokenSource query01CancelControl = new(delayQueryCancel);
                object resQuery01 = await workflow.QueryAsync<object, string>("Some-Query-01",
                                                                              "Some-Query-Argument",
                                                                              cancelToken: query01CancelControl.Token);

                Console.WriteLine("Query sent. Look for it in the workflow history.");
                Console.WriteLine($"Query result: |{Format.QuoteIfString(resQuery01)}|.");
            }
            catch (OperationCanceledException opCncldEx)
            {
                Console.WriteLine("Received expected exception.");
                Console.WriteLine(opCncldEx.TypeAndMessage());

                Exception innerEx = opCncldEx.InnerException;
                while (innerEx != null)
                {
                    Console.WriteLine("\n  Inner --> " + innerEx.TypeAndMessage());
                    innerEx = innerEx.InnerException;
                }
            }

            Console.WriteLine();
            Console.WriteLine("Sending cancellation request to a workflow...");
            Console.WriteLine();

            await workflow.RequestCancellationAsync();

            Console.WriteLine("Cancellation requested. However, it the workflow was not designed to honor it, so it will remain ignored. Look for the request in the workflow history.");
            Console.WriteLine("Look for the request in the workflow history.");

            _ = Task.Run(async () =>
                {
                    TimeSpan delayTermination = TimeSpan.FromSeconds(2);
                    Console.WriteLine($"Started automatic termination invoker with a delay of '{delayTermination}'.");

                    await Task.Delay(delayTermination);
                    Console.WriteLine($"Delay of {delayTermination} elapsed. Terminating workflow...");

                    await workflow.TerminateAsync("Good-reason-for-termination", details: DateTimeOffset.Now);
                    Console.WriteLine($"Workflow terminated.");
                });

            Console.WriteLine();
            Console.WriteLine("Waiting for result...");
            Console.WriteLine();

            try
            {
                await workflow.GetResultAsync<object>();

                throw new Exception("ERROR. We should never get here, because the above code is expected to throw.");
            }
            catch (WorkflowConcludedAbnormallyException wcaEx) when (wcaEx.ConclusionStatus == WorkflowExecutionStatus.Terminated)
            {
                Console.WriteLine("Received expected exception.");
                Console.WriteLine(wcaEx.TypeAndMessage());

                Exception innerEx = wcaEx.InnerException;
                while (innerEx != null)
                {
                    Console.WriteLine("\n  Inner --> " + innerEx.TypeAndMessage());
                    innerEx = innerEx.InnerException;
                }
            }

            Console.WriteLine();
            Console.WriteLine("Creating a handle to a non-existing workflow...");

            IWorkflowHandle workflow3 = client.CreateWorkflowHandle("Non-Existing-Workflow-Id");

            Console.WriteLine("Created. Info:");
            Console.WriteLine($"    Namespace:       {workflow3.Namespace}");
            Console.WriteLine($"    WorkflowId:      {workflow3.WorkflowId}");
            Console.WriteLine($"    IsBound:         {workflow3.IsBound}");

            Console.WriteLine();
            Console.WriteLine("Verifying existence...");
            Console.WriteLine();

            bool wfExists = await workflow3.ExistsAsync();

            Console.WriteLine($"Verified. {nameof(wfExists)}={wfExists}.");
            Console.WriteLine("Updated handle info:");
            Console.WriteLine($"    IsBound:         {workflow3.IsBound}");

            try
            {
                Console.WriteLine($"    WorkflowChainId: {workflow3.WorkflowChainId}");

                throw new Exception("ERROR. We should never get here, because the above code is expected to throw.");
            }
            catch (InvalidOperationException invOpEx)
            {
                Console.WriteLine($"    Expected exception while getting {nameof(workflow3.WorkflowChainId)}:");
                Console.WriteLine($"    --> {invOpEx.TypeAndMessage()}");

                Exception innerEx = invOpEx.InnerException;
                while (innerEx != null)
                {
                    Console.WriteLine("\n      Inner --> " + invOpEx.TypeAndMessage());
                    innerEx = innerEx.InnerException;
                }
            }

            Console.WriteLine();
            Console.WriteLine("Sending signal to a non-existing workflow...");
            Console.WriteLine();

            try
            {
                await workflow3.SignalAsync("Some-Signal-02");

                throw new Exception("ERROR. We should never get here, because the above code is expected to throw.");
            }
            catch (WorkflowNotFoundException wnfEx)
            {
                Console.WriteLine("Received expected exception.");
                Console.WriteLine(wnfEx.TypeAndMessage());

                Exception innerEx = wnfEx.InnerException;
                while (innerEx != null)
                {
                    Console.WriteLine("\n  Inner --> " + innerEx.TypeAndMessage());
                    innerEx = innerEx.InnerException;
                }
            }

            Console.WriteLine();
            Console.WriteLine("Waiting for result of a non-existing workflow...");
            Console.WriteLine();

            try
            {
                await workflow3.GetResultAsync<object>();

                throw new Exception("ERROR. We should never get here, because the above code is expected to throw.");
            }
            catch (WorkflowNotFoundException wnfEx)
            {
                Console.WriteLine("Received expected exception.");
                Console.WriteLine(wnfEx.TypeAndMessage());

                Exception innerEx = wnfEx.InnerException;
                while (innerEx != null)
                {
                    Console.WriteLine("\n  Inner --> " + innerEx.TypeAndMessage());
                    innerEx = innerEx.InnerException;
                }
            }
        }
    }
}
