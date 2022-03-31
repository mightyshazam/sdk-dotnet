using Temporal.Api.Common.V1;

namespace Temporal.Serialization
{
    public interface IPayloadConverter
    {
        bool TryDeserialize<T>(Payload serializedData, out T item);
        bool TrySerialize<T>(T item, out Payload serializedData);
    }
}
