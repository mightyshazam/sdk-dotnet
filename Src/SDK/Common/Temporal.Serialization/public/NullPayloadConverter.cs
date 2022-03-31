using System;
using Candidly.Util;
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

        public bool TryDeserialize<T>(Payload serializedData, out T item)
        {
            item = default(T);

            if (serializedData == null)
            {
                return false;
            }

            if (item == null
                    && serializedData.Metadata.TryGetValue(PayloadConverter.PayloadMetadataEncodingKey, out ByteString encodingBytes)
                    && PayloadMetadataEncodingValueBytes.Equals(encodingBytes))
            {
                return true;
            }

            return false;
        }

        public bool TrySerialize<T>(T item, out Payload serializedData)
        {
            if (item == null)
            {
                serializedData = new Payload();
                serializedData.Metadata.Add(PayloadConverter.PayloadMetadataEncodingKey, PayloadMetadataEncodingValueBytes);
                return true;
            }

            serializedData = null;
            return false;
        }
    }
}
