using System;
using Candidly.Util;
using Google.Protobuf;
using Temporal.Api.Common.V1;

namespace Temporal.Serialization
{
    /// <summary>
    /// Mainly intended for debugging and development.
    /// </summary>
    public class CatchAllPayloadConverter : IPayloadConverter
    {
        public const string PayloadMetadataEncodingValue = "json/plain";

        private static ByteString s_payloadMetadataEncodingValueBytes = null;

        private static ByteString PayloadMetadataEncodingValueBytes
        {
            get { return PayloadConverter.GetOrCreateBytes(PayloadMetadataEncodingValue, ref s_payloadMetadataEncodingValueBytes); }
        }

        public bool TryDeserialize<T>(Payload serializedData, out T item)
        {
            item = default(T);

            if (serializedData == null)
            {
                return true;
            }

            try
            {
                string dataStr = serializedData.Data.ToStringUtf8();
                item = dataStr.Cast<string, T>();
            }
            catch { }

            return false;
        }

        public bool TrySerialize<T>(T item, out Payload serializedData)
        {
            string itemString = (item == null) ? "null" : item.ToString();
            string itemInfo = $"\"[{item.TypeOf()}]{{{itemString}}}\"";

            serializedData = new Payload();
            serializedData.Metadata.Add(PayloadConverter.PayloadMetadataEncodingKey, PayloadMetadataEncodingValueBytes);
            serializedData.Data = ByteString.CopyFromUtf8(itemInfo);

            return true;
        }
    }
}
