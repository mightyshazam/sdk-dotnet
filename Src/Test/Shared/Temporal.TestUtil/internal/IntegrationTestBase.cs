using System.Threading;
using System.Threading.Tasks;

using Xunit.Abstractions;

using Temporal.TestUtil;

namespace Temporal.Sdk.WorkflowClient.Test.Integration
{
    public class IntegrationTestBase : TestBase
    {
        private ITemporalTestServerController _testServer = null;

        public IntegrationTestBase(ITestOutputHelper cout)
            : base(cout)
        {
        }

        internal virtual ITemporalTestServerController TestServer
        {
            get { return _testServer; }
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            // In the future, when we will have other test servers (docker base, remote, ...), we will use some sort of
            // configuration mechanism to decide on the implementation chosen.

            ITemporalTestServerController testServer = new JavaBasedTemporalTestServerController();
            await testServer.StartAsync();
        }

        public override async Task DisposeAsync()
        {
            ITemporalTestServerController testServer = Interlocked.Exchange(ref _testServer, null);
            if (testServer != null)
            {
                await testServer.ShutdownAsync();
            }

            await base.DisposeAsync();
        }
    }
}