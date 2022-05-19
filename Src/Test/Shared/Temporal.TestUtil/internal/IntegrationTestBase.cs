using System.Threading;
using System.Threading.Tasks;

using Xunit.Abstractions;

namespace Temporal.TestUtil
{
    public class IntegrationTestBase : TestBase
    {
        protected const string CaCertificatePath = "";
        protected const string ClientCertificatePath = "";
        protected const string ClientKeyPath = "";
        protected const string ServerCertificatePath = "";
        protected const string ServerKeyPath = "";

        private const bool RedirectServerOutToCoutDefault = false;

        private readonly bool _redirectServerOutToCout;
        private ITemporalTestServerController _testServer = null;

        private readonly TestCaseContextMonikers _testCaseContextMonikers;

        public IntegrationTestBase(ITestOutputHelper cout, int temporalServicePort, TestTlsOptions testTlsOptions)
            : this(cout, RedirectServerOutToCoutDefault, temporalServicePort, testTlsOptions)
        {
        }

        public IntegrationTestBase(ITestOutputHelper cout, bool redirectServerOutToCout, int temporalServicePort, TestTlsOptions testTlsOptions)
            : base(cout)
        {
            _redirectServerOutToCout = redirectServerOutToCout;
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

            _testServer = new TemporalLiteExeTestServerController(Cout, _redirectServerOutToCout);
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