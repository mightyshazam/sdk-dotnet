using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Temporal.Api.Enums.V1;
using Temporal.TestUtil;
using Temporal.WorkflowClient;
using Temporal.WorkflowClient.Interceptors;
using Xunit;
using Xunit.Abstractions;

namespace Temporal.Sdk.WorkflowClient.Test.Int
{
    [Collection("SequentialTestExecution")]
    public class WorkflowRunHandleTest : IntegrationTestBase
    {
        private TemporalClient _client;

        public WorkflowRunHandleTest(ITestOutputHelper cout)
            : base(cout, 7233, TestTlsOptions.None)
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            TemporalClient client = CreateTemporalClient();
            _client = client;
        }

        public override async Task DisposeAsync()
        {
            ITemporalClient client = Interlocked.Exchange(ref _client, null);
            if (client != null)
            {
                client.Dispose();
            }

            await base.DisposeAsync();
        }

        [Fact]
        public Task RequestCancellationAsync()
        {
            return DoWithHandle(async (handle, _) =>
            {
                using CancellationTokenSource source = new();
                source.CancelAfter(TimeSpan.FromSeconds(10));
                Task<IWorkflowRunResult> conclusionTask = handle.AwaitConclusionAsync(source.Token);
                await handle.RequestCancellationAsync(source.Token);
                IWorkflowRunResult result = await conclusionTask;
                result.IsConcludedSuccessfully.Should().BeTrue();
                result.Status.Should().Be(WorkflowExecutionStatus.Canceled);
            });
        }

        private (string workflowId, string queue) GetIdAndQueue([CallerMemberName] string caller = null)
        {
            return (TestCaseWorkflowId(caller), TestCaseTaskQueue(caller));
        }

        private async Task DoWithHandle(Func<WorkflowRunHandle, string, Task> work, [CallerMemberName] string caller = null)
        {
            (string workflowId, string queue) = GetIdAndQueue(caller);
            TemporalClient client = CreateTemporalClient();
            using WorkflowHandle handle = WorkflowHandle.CreateUnbound(client, workflowId);
            StartWorkflow.Result result = await handle.StartAsync(caller, queue, "hello");
            using WorkflowRunHandle runHandle = new(handle, result.WorkflowRunId);
            await work(runHandle, queue);
        }
    }
}