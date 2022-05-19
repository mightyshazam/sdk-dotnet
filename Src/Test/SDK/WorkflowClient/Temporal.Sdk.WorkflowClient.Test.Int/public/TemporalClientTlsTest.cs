using Temporal.TestUtil;
using Xunit.Abstractions;

namespace Temporal.Sdk.WorkflowClient.Test.Int
{
    // ReSharper disable once UnusedType.Global
    public class TemporalClientTlsTest : AbstractTemporalClientTest
    {
        public TemporalClientTlsTest(ITestOutputHelper cout)
            : base(cout, TestTlsOptions.Server)
        {
        }
    }
}