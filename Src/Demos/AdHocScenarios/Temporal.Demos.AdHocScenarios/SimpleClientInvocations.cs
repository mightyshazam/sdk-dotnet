using System;
using System.Threading.Tasks;
using Temporal.Common;
using Temporal.WorkflowClient;

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
            ITemporalClient client = null;
            IDataValue arg = null;

            await client.StartWorkflowAsync("workflowId",
                                            "workflowTypeName",
                                            "taskQueue",
                                            null);

            await client.StartWorkflowWithSignalAsync("workflowId",
                                            "workflowTypeName",
                                            "taskQueue",
                                            "workflowArg",
                                            "signamName",
                                            "signalArg");
        }

    }
}
