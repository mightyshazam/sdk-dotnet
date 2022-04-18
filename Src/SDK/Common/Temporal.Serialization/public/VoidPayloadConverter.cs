using System;
using Temporal.Util;
using Temporal.Api.Common.V1;

namespace Temporal.Serialization
{
    public class VoidPayloadConverter : IPayloadConverter
    {
        public bool TryDeserialize<T>(Payloads serializedData, out T item)
        {
            // Check: `serializedData` is not null
            //      AND `serializedData` has ZERO payload entries
            //      AND `T` represents the `IPayload.Void` type:

            if (serializedData != null
                    && SerializationUtil.GetPayloadCount(serializedData) == 0
                    && typeof(T) == typeof(Temporal.Common.IPayload.Void))
            {
                item = Temporal.Common.Payload.Void.Cast<Temporal.Common.IPayload.Void, T>();
                return true;
            }

            item = default(T);
            return true;
        }

        public bool TrySerialize<T>(T item, Payloads serializedDataAccumulator)
        {
            // If the specified `item` is `IPayload.Void`, then perform the serialization,
            // but the serialization does not actually generate a payload entry.

            if (item != null && item is Temporal.Common.IPayload.Void)
            {
                return true;
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            return Object.ReferenceEquals(this, obj) || ((obj != null) && this.GetType().Equals(obj.GetType()));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
