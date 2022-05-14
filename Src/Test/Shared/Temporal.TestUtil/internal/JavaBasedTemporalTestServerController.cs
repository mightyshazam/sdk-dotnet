using System.Threading;
using System.Threading.Tasks;

namespace Temporal.TestUtil
{
    internal class JavaBasedTemporalTestServerController : ITemporalTestServerController
    {
        public Task StartAsync(CancellationToken cancelToken = default)
        {
            return null;
        }

        public Task ShutdownAsync(CancellationToken cancelToken = default)
        {
            return null;
        }
    }
}
