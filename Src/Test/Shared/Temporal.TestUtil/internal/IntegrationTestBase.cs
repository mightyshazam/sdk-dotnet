using System.Threading;
using System.Threading.Tasks;

using Xunit.Abstractions;

namespace Temporal.TestUtil
{
    public class IntegrationTestBase : TestBase
    {
        private const bool RedirectServerOutToTstoutDefault = false;

        private readonly bool _redirectServerOutToTstout;
        private ITemporalTestServerController _testServer = null;

        private readonly TestCaseContextMonikers _testCaseContextMonikers;

        public IntegrationTestBase(ITestOutputHelper tstout, int temporalServicePort, TestTlsOptions testTlsOptions)
            : this(tstout, RedirectServerOutToTstoutDefault, temporalServicePort, testTlsOptions)
        {
        }

        public IntegrationTestBase(ITestOutputHelper cout, bool redirectServerOutToTstout, int temporalServicePort, TestTlsOptions testTlsOptions)
            : base(cout)
        {
            _redirectServerOutToTstout = redirectServerOutToTstout;
            Port = temporalServicePort;
            TlsOptions = testTlsOptions;
            _testCaseContextMonikers = new TestCaseContextMonikers(System.DateTimeOffset.Now);
        }

        protected TestTlsOptions TlsOptions { get; }

        protected int Port { get; }

        internal virtual ITemporalTestServerController TestServer
        {
            get { return _testServer; }
        }

        internal virtual TestCaseContextMonikers TestCaseContextMonikers
        {
            get { return _testCaseContextMonikers; }
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            // In the future, when we will have other test servers (docker base, remote, ...), we will use some sort of
            // configuration mechanism to decide on the implementation chosen.

            _testServer = new TemporalLiteExeTestServerController(Tstout, _redirectServerOutToTstout);
            await _testServer.StartAsync(TlsOptions, Port);
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