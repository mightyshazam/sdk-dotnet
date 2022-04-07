using System.Threading;
using System.Threading.Tasks;
using Temporal.Api.Common.V1;

namespace Temporal.Serialization
{
    public interface IDataConverter
    {
        Task<T> DeserializeAsync<T>(Payloads serializedData, CancellationToken cancelToken);
        Task<Payloads> SerializeAsync<T>(T item, CancellationToken cancelToken);
    }
}
