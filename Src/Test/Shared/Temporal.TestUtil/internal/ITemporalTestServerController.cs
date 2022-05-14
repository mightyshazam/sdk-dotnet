using System.Threading;
using System.Threading.Tasks;

namespace Temporal.TestUtil
{
    internal interface ITemporalTestServerController
    {
        Task StartAsync(CancellationToken cancelToken = default);
        Task ShutdownAsync(CancellationToken cancelToken = default);
    }
}
