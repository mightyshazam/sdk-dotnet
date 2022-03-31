using System;
using System.Threading.Tasks;
using Candidly.Util;
using Temporal.Api.Enums.V1;
using Temporal.WorkflowClient;
using Temporal.WorkflowClient.Errors;

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
            IWorkflowChain workflow = await client.StartWorkflowAsync(demoWfId,
                                                                      "DemoWorkflowTypeName",
                                                                      "DemoTaskQueue");
            Console.WriteLine("Started. Info:");
            Console.WriteLine($"    Namespace:       {workflow.Namespace}");
            Console.WriteLine($"    WorkflowId:      {workflow.WorkflowId}");
            Console.WriteLine($"    IsBound:         {workflow.IsBound}");
            Console.WriteLine($"    WorkflowChainId: {workflow.WorkflowChainId}");

            Console.WriteLine();
            Console.WriteLine("Starting again...");
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
            }

            Console.WriteLine();
            Console.WriteLine("Creating a handle to the existing workflow...");

            IWorkflowChain workflow2 = client.CreateWorkflowHandle(demoWfId);

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
                Console.WriteLine($"    {invOpEx.TypeAndMessage()}");
            }

            Console.WriteLine();
            Console.WriteLine("Now, Starting-If-Not-Running...");
            Console.WriteLine();

            bool canStart = await workflow2.StartIfNotRunningAsync("DemoWorkflowTypeName", "DemoTaskQueue");

            Console.WriteLine($"Result: {canStart} ({(canStart ? "NOT expected" : "EXPECTED")}).");
            if (canStart)
            {
                throw new Exception("ERROR. StartIfNotRunningAsync was not expected to succeed.");
            }

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
            }
        }
    }
}
