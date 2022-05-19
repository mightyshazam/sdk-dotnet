using System.Threading.Tasks;

namespace Temporal.TestUtil
{
    internal interface ITemporalTestServerController
    {
        Task StartAsync(TestTlsOptions testTlsOptions, int port = 7233);
        Task ShutdownAsync();
    }
}
