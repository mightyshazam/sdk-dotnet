using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

using Xunit;
using Xunit.Abstractions;

using Temporal.TestUtil;
using Temporal.WorkflowClient;
using Temporal.Api.History.V1;
using FluentAssertions;
using Temporal.Api.Enums.V1;

namespace Temporal.Sdk.WorkflowClient.Test.Int
{
    public class TemporalClientTest : IntegrationTestBase
    {
        private ITemporalClient _client = null;
        private ExtendedWorkflowServiceClient _wfServiceClient = null;

        public TemporalClientTest(ITestOutputHelper cout)
            : base(cout)
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            TemporalClient client = new();
            _client = client;

            _wfServiceClient = new ExtendedWorkflowServiceClient(client.Configuration);
        }

        public override async Task DisposeAsync()
        {
            ExtendedWorkflowServiceClient wfServiceClient = Interlocked.Exchange(ref _wfServiceClient, null);
            if (wfServiceClient != null)
            {
                wfServiceClient.Dispose();
            }

            ITemporalClient client = Interlocked.Exchange(ref _client, null);
            if (client != null)
            {
                client.Dispose();
            }

            await base.DisposeAsync();
        }

        private string TestCaseWorkflowId([CallerMemberName] string testMethodName = null)
        {
            return TestCaseContextMonikers.ForWorkflowId(this, testMethodName);
        }

        private string TestCaseTaskQueue([CallerMemberName] string testMethodName = null)
        {
            return TestCaseContextMonikers.ForTaskQueue(this, testMethodName);
        }


        [Fact]
        public async Task ConnectAsync()
        {
            await Task.Delay(1);
        }

        [Fact]
        public void Ctor_Plain()
        {
        }

        [Fact]
        public void Ctor_WithClientConfiguration()
        {

        }

        [Fact]
        public async Task StartWorkflowAsync_NoWfArgs()
        {
            const string WfTypeName = "TestWorkflowTypeName";
            string wfId = TestCaseWorkflowId();
            string taskQueue = TestCaseTaskQueue();

            await _client.StartWorkflowAsync(wfId, WfTypeName, taskQueue);

            List<HistoryEvent> history = await _wfServiceClient.GetHistoryAsync(wfId, 2);

            history.Should().NotBeNull();
            history.Should().HaveCount(2);

            history[0].Should().NotBeNull();
            history[0].EventType.Should().Be(EventType.WorkflowExecutionStarted);

            history[0].WorkflowExecutionStartedEventAttributes.Should().NotBeNull();
            history[0].WorkflowExecutionStartedEventAttributes.WorkflowType?.Name.Should().Be(WfTypeName);
            history[0].WorkflowExecutionStartedEventAttributes.TaskQueue?.Name.Should().Be(taskQueue);
            //history[0].WorkflowExecutionStartedEventAttributes.Input
            history[0].WorkflowExecutionStartedEventAttributes.FirstExecutionRunId.Should().NotBeNullOrWhiteSpace();

        }

        [Fact]
        public async Task StartWorkflowAsync_WithWfArgs()
        {
            await Task.Delay(1);
        }

    }
}

