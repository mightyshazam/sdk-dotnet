using System;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

using Temporal.TestUtil;

namespace Temporal.Sdk.WorkflowClient.Test.Int
{
    public class TemporalClientTest : IntegrationTestBase
    {
        public TemporalClientTest(ITestOutputHelper cout)
            : base(cout)
        {
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
            await Task.Delay(1);
        }

        [Fact]
        public async Task StartWorkflowAsync_WithWfArgs()
        {
            await Task.Delay(1);
            Cout.WriteLine("Current dir xxx:" + Environment.CurrentDirectory);
        }

    }
}