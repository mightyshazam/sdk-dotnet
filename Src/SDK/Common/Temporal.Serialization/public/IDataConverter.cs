using Temporal.Api.Common.V1;

namespace Temporal.Serialization
{
    public interface IDataConverter
    {
        T Deserialize<T>(Payloads serializedData);
        Payloads Serialize<T>(T item);
    }
}
