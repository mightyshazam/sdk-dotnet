using Xunit.Abstractions;
using Temporal.TestUtil;

namespace Temporal.Sdk.WorkflowClient.Test.Int
{
    // ReSharper disable once UnusedType.Global
    public class TemporalClientTest : AbstractTemporalClientTest
    {
        public TemporalClientTest(ITestOutputHelper cout)
            : base(cout, TestTlsOptions.None)
        {
        }
    }
}

