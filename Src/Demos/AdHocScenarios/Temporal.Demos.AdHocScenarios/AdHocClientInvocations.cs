using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Temporal.Util;
using Temporal.Api.Enums.V1;
using Temporal.WorkflowClient;
using Temporal.WorkflowClient.Errors;

namespace Temporal.Demos.AdHocScenarios
{
    internal class AdHocClientInvocations
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

            Console.WriteLine();
            Console.WriteLine("Creating a handle to the existing workflow...");

            const string demoWfId = "qqq";
            IWorkflowHandle workflow = client.CreateWorkflowHandle(demoWfId);

            Console.WriteLine("Created. Info:");
            Console.WriteLine($"    Namespace:       {workflow.Namespace}");
            Console.WriteLine($"    WorkflowId:      {workflow.WorkflowId}");
            Console.WriteLine($"    IsBound:         {workflow.IsBound}");

            try
            {
                Console.WriteLine($"    WorkflowChainId: {workflow.WorkflowChainId}");

                throw new Exception("ERROR. We should never get here, because the above code is expected to throw.");
            }
            catch (InvalidOperationException invOpEx)
            {
                Console.WriteLine($"    Expected exception while getting {nameof(workflow.WorkflowChainId)}:");
                Console.WriteLine($"    --> {invOpEx.TypeAndMessage()}");

                Exception innerEx = invOpEx.InnerException;
                while (innerEx != null)
                {
                    Console.WriteLine("\n      Inner --> " + invOpEx.TypeAndMessage());
                    innerEx = innerEx.InnerException;
                }
            }

            Console.WriteLine();
            Console.WriteLine("Waiting for result...");
            Console.WriteLine();

            try
            {
                await workflow.GetResultAsync<object>();

                throw new Exception("ERROR. We should never get here, because the above code is expected to throw.");
            }
            catch (WorkflowConcludedAbnormallyException wcaEx) when (wcaEx.ConclusionStatus == WorkflowExecutionStatus.Failed)
            {
                Console.WriteLine("Received expected exception.");
                Console.WriteLine(wcaEx.TypeAndMessage());

                Exception innerEx = wcaEx.InnerException;
                while (innerEx != null)
                {
                    Console.WriteLine("\n  Inner --> " + innerEx.TypeAndMessage());
                    innerEx = innerEx.InnerException;
                }

                Console.WriteLine();
                Console.WriteLine("Rethrowing the caught exception.");
                Console.WriteLine();
                ExceptionDispatchInfo.Capture(wcaEx).Throw();
            }

            Console.WriteLine();
        }
    }
}
