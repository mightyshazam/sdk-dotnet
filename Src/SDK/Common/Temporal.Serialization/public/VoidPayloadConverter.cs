using Candidly.Util;
using Temporal.Api.Common.V1;

namespace Temporal.Serialization
{
    public class VoidPayloadConverter : IPayloadConverter
    {
        public bool TryDeserialize<T>(Payload serializedData, out T item)
        {
            if (serializedData == null && typeof(T) == typeof(Temporal.Common.IPayload.Void))
            {
                item = Temporal.Common.Payload.Void.Cast<Temporal.Common.IPayload.Void, T>();
                return true;
            }

            item = default(T);
            return true;
        }

        public bool TrySerialize<T>(T item, out Payload serializedData)
        {
            serializedData = null;

            if (item != null && item.GetType() == typeof(Temporal.Common.IPayload.Void))
            {
                return true;
            }

            return false;
        }
    }
}
