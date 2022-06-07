using Xunit.Abstractions;
using Temporal.TestUtil;

namespace Temporal.Sdk.WorkflowClient.Test.Int
{
    // ReSharper disable once UnusedType.Global
    public class TemporalClientTest : TemporalClientTestBase
    {
        public TemporalClientTest(ITestOutputHelper tstOut)
            : base(tstOut, TestTlsOptions.None)
        {
        }
    }
}

