using System;
using System.Collections.Generic;
using Temporal.Util;
using Google.Protobuf;
using Temporal.Api.Common.V1;

namespace Temporal.Serialization
{
    public class NullPayloadConverter : IPayloadConverter
    {
        public const string PayloadMetadataEncodingValue = "binary/null";

        private static ByteString s_payloadMetadataEncodingValueBytes = null;

        private static ByteString PayloadMetadataEncodingValueBytes
        {
            get { return PayloadConverter.GetOrCreateBytes(PayloadMetadataEncodingValue, ref s_payloadMetadataEncodingValueBytes); }
        }

        public bool TryDeserialize<T>(Payloads serializedData, out T item)
        {
            item = default(T);

            // Check: `Payloads` have exactly one entry:
            if (SerializationUtil.TryGetSinglePayload(serializedData, out Payload serializedItem))
            {
                // Check: `T` is nullable AND the payloads entry has the matching encoding key in the metadata:
                if (item == null
                        && serializedItem.Metadata.TryGetValue(PayloadConverter.PayloadMetadataEncodingKey, out ByteString encodingBytes)
                        && PayloadMetadataEncodingValueBytes.Equals(encodingBytes))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TrySerialize<T>(T item, Payloads serializedDataAccumulator)
        {
            if (item == null)
            {
                Payload serializedItemData = new();
                serializedItemData.Metadata.Add(PayloadConverter.PayloadMetadataEncodingKey, PayloadMetadataEncodingValueBytes);

                SerializationUtil.Add(serializedDataAccumulator, serializedItemData);
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
