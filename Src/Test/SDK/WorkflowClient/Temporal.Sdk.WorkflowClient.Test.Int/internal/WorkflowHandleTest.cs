using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Grpc.Core;
using Temporal.Api.Enums.V1;
using Temporal.Api.WorkflowService.V1;
using Temporal.TestUtil;
using Temporal.WorkflowClient;
using Temporal.WorkflowClient.Errors;
using Temporal.WorkflowClient.Interceptors;
using Xunit;
using Xunit.Abstractions;

namespace Temporal.Sdk.WorkflowClient.Test.Int
{
    [Collection("SequentialTestExecution")]
    public class WorkflowHandleTest : IntegrationTestBase
    {
        private TemporalClient _client;

        public WorkflowHandleTest(ITestOutputHelper cout)
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
        public Task ExistsAsync_BeforeStart()
        {
            return DoWithHandle(async (handle, _) =>
            {
                bool exists = await handle.ExistsAsync(CancellationToken.None);
                exists.Should().BeFalse();
            });
        }

        [Fact]
        public Task ExistsAsync_AfterStart()
        {
            return DoWithStartedHandle(async (handle, _) =>
            {
                bool exists = await handle.ExistsAsync(CancellationToken.None);
                exists.Should().BeTrue();
            },
                "hello");
        }

        [Fact]
        public Task EnsureBoundAsync_BeforeStart()
        {
            return DoWithHandle(async (handle, _) =>
            {
                Func<Task> act = () => handle.EnsureBoundAsync(CancellationToken.None);
                Func<string> getChainId = () => handle.WorkflowChainId;
                await act.Should().ThrowAsync<WorkflowNotFoundException>();
                getChainId.Should().Throw<InvalidOperationException>();
            });
        }

        [Fact]
        public Task EnsureBoundAsync_AfterStart()
        {
            return DoWithHandle(
                async (handle, queue) =>
                {
                    handle.IsBound.Should().BeFalse();
                    await handle.StartAsync(nameof(EnsureBoundAsync_AfterStart),
                        queue,
                        "hello");
                    await handle.EnsureBoundAsync(CancellationToken.None);
                    handle.WorkflowChainId.Should().NotBeNull();
                    handle.IsBound.Should().BeTrue();
                    using WorkflowHandle bound = WorkflowHandle.CreateBound(handle.TemporalServiceClient,
                        handle.WorkflowId,
                        handle.WorkflowChainId);
                    bool exists = await bound.ExistsAsync(CancellationToken.None);
                    exists.Should().BeTrue();
                    bound.WorkflowId.Should().Be(handle.WorkflowId, "They reference the same workflow");
                    bound.WorkflowChainId.Should().Be(handle.WorkflowChainId, "They reference the same workflow");
                    bound.IsBound.Should().BeTrue();
                });
        }

        [Fact]
        public Task SignalAsync_BeforeStart()
        {
            return DoWithHandle(async (handle, _) =>
            {
                Func<Task> act = () => handle.SignalAsync("test", CancellationToken.None);
                await act.Should().ThrowAsync<WorkflowNotFoundException>();
            });
        }

        [Fact]
        public Task SignalAsync_AfterStart()
        {
            return DoWithStartedHandle((handle, _) => handle.SignalAsync("test", CancellationToken.None),
                "hello");
        }

        [Fact]
        public Task TerminateAsync_BeforeStart()
        {
            return DoWithHandle(async (handle, _) =>
            {
                Func<Task> act = () => handle.TerminateAsync("test", CancellationToken.None);
                await act.Should().ThrowAsync<WorkflowNotFoundException>();
            });
        }

        [Fact]
        public Task TerminateAsync_AfterStart()
        {
            return DoWithStartedHandle((handle, _) => handle.TerminateAsync("test", CancellationToken.None),
                "hello");
        }

        [Fact]
        public Task TerminateAsync_AfterStartWithoutArguments()
        {
            return DoWithStartedHandle((handle, _) => handle.TerminateAsync(),
                "hello");
        }

        [Fact]
        public Task SignalWithStartAsync()
        {
            return DoWithHandle(async (handle, queue) =>
            {
                StartWorkflow.Result result = await handle.SignalWithStartAsync(nameof(SignalWithStartAsync),
                    queue,
                    "hello",
                    "test_signal",
                    "hello");
                result.Should().NotBeNull();
                result.StatusCode.Should().Be(StatusCode.OK);
            });
        }

        [Fact]
        public Task GetStatusAsync_BeforeStart()
        {
            return DoWithHandle(async (handle, _) =>
            {
                Func<Task> act = () => handle.GetStatusAsync(CancellationToken.None);
                await act.Should().ThrowAsync<WorkflowNotFoundException>();
            });
        }

        [Fact]
        public Task GetStatusAsync_AfterStart()
        {
            return DoWithStartedHandle(async (handle, _) =>
                {
                    WorkflowExecutionStatus status = await handle.GetStatusAsync(CancellationToken.None);
                    status.Should().Be(WorkflowExecutionStatus.Running);
                },
                "hello");
        }

        [Fact]
        public Task DescribeAsync_BeforeStart()
        {
            return DoWithHandle(async (handle, _) =>
            {
                Func<Task> act = () => handle.DescribeAsync(CancellationToken.None);
                await act.Should().ThrowAsync<WorkflowNotFoundException>();
            });
        }

        [Fact]
        public Task DescribeAsync_AfterStart()
        {
            return DoWithStartedHandle(async (handle, taskQueue) =>
                {
                    DescribeWorkflowExecutionResponse status = await handle.DescribeAsync(CancellationToken.None);
                    status.Should().NotBeNull();
                    status.ExecutionConfig.Should().NotBeNull();
                    status.PendingActivities.Should().BeEmpty();
                    status.PendingChildren.Should().BeEmpty();
                    status.WorkflowExecutionInfo.Should().NotBeNull();
                    status.ExecutionConfig.TaskQueue.Name.Should().Be(taskQueue);
                    status.WorkflowExecutionInfo.TaskQueue.Should().Be(taskQueue);
                    status.WorkflowExecutionInfo.Execution.WorkflowId.Should().Be(handle.WorkflowId);
                },
                "hello");
        }

        private (string workflowId, string queue) GetIdAndQueue([CallerMemberName] string caller = null)
        {
            return (TestCaseWorkflowId(caller), TestCaseTaskQueue(caller));
        }

        // TODO: Explain usage
        private Task DoWithStartedHandle<T>(Func<WorkflowHandle, string, Task> work,
            T arg,
            [CallerMemberName] string caller = null)
        {
            return DoWithHandle(
                async (handle, queue) =>
                {
                    await handle.StartAsync(caller, queue, arg);
                    await work(handle, queue);
                },
                caller);
        }

        // TODO: Explain usage
        private async Task DoWithHandle(Func<WorkflowHandle, string, Task> work, [CallerMemberName] string caller = null)
        {
            (string workflowId, string queue) = GetIdAndQueue(caller);
            TemporalClient client = CreateTemporalClient();
            using WorkflowHandle handle = WorkflowHandle.CreateUnbound(client, workflowId);
            await work(handle, queue);
        }
    }
}