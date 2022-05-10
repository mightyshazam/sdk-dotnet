using System;
using Google.Protobuf;
using Newtonsoft.Json;
using Temporal.Api.Common.V1;

namespace Temporal.Serialization
{
    public class NewtonsoftJsonPayloadConverter : IPayloadConverter
    {
        public const string PayloadMetadataEncodingValue = "json/plain";

        private static ByteString s_payloadMetadataEncodingValueBytes = null;

        private static ByteString PayloadMetadataEncodingValueBytes
        {
            get { return PayloadConverter.GetOrCreateBytes(PayloadMetadataEncodingValue, ref s_payloadMetadataEncodingValueBytes); }
        }

        private static readonly JsonSerializerSettings s_jsonSerializerSettings = new()
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateParseHandling = DateParseHandling.DateTimeOffset,
            DefaultValueHandling = DefaultValueHandling.Include,
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
        };

        public bool TryDeserialize<T>(Payloads serializedData, out T item)
        {
            if (SerializationUtil.TryGetSinglePayload(serializedData, out Payload serializedItem)
                    && serializedItem.Metadata.TryGetValue(PayloadConverter.PayloadMetadataEncodingKey, out ByteString encodingBytes)
                    && PayloadMetadataEncodingValueBytes.Equals(encodingBytes))
            {
                string itemJson = serializedItem.Data.ToStringUtf8();
                item = JsonConvert.DeserializeObject<T>(itemJson, s_jsonSerializerSettings);
                return true;
            }

            item = default(T);
            return false;
        }

        public bool TrySerialize<T>(T item, Payloads serializedDataAccumulator)
        {
            if (item != null)
            {
                string itemJson = JsonConvert.SerializeObject(item, Formatting.Indented, s_jsonSerializerSettings);

                Payload serializedItemData = new();
                serializedItemData.Metadata.Add(PayloadConverter.PayloadMetadataEncodingKey, PayloadMetadataEncodingValueBytes);
                serializedItemData.Data = ByteString.CopyFromUtf8(itemJson);

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
