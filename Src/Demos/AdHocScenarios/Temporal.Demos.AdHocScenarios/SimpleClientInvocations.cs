using System;
using System.Threading.Tasks;
using Temporal.Util;
using Temporal.Api.Enums.V1;
using Temporal.Common;
using Temporal.WorkflowClient;
using Temporal.WorkflowClient.Errors;
using Temporal.WorkflowClient.Interceptors;

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
            Console.WriteLine("Waiting for result...");
            Console.WriteLine();

            try
            {
                // At this point, we expect the user to manually terminate the workflow via the UI
                // (or course, manual interventions need to be removed in the mid-term).
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
