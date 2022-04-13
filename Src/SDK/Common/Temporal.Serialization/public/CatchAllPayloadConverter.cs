using System;
using System.Text;
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

        public bool TryDeserialize<T>(Payloads serializedData, out T item)
        {
            StringBuilder msg = new();

            if (serializedData == null)
            {
                msg.Append(nameof(serializedData));
                msg.Append(" is null.");
            }
            else if (serializedData.Payloads_ == null)
            {
                msg.Append(nameof(serializedData));
                msg.Append(".");
                msg.Append(nameof(serializedData.Payloads_));
                msg.Append(" is null.");
            }
            else
            {
                int countEntries = SerializationUtil.GetPayloadCount(serializedData);

                msg.Append(nameof(serializedData));
                msg.Append(".");
                msg.Append(nameof(serializedData.Payloads_));
                msg.Append(" contains");
                msg.Append(countEntries);
                msg.Append(" entries: ");
                msg.Append(" [");

                bool first = true;
                foreach (Payload payload in serializedData.Payloads_)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        msg.Append(", ");
                    }

                    try
                    {
                        msg.Append(Format.QuoteOrNull(payload?.Data?.ToStringUtf8()));
                    }
                    catch (Exception ex)
                    {
                        msg.Append(ex.TypeAndMessage());
                    }
                }

                msg.Append(" ]");
            }

            if (!msg.ToString().TryCast<string, T>(out item))
            {
                item = default(T);
            }

            return true;
        }

        public bool TrySerialize<T>(T item, Payloads serializedDataAccumulator)
        {
            string itemString = (item == null) ? "null" : item.ToString();
            string itemInfo = $"\"[{item.TypeOf()}]{{{itemString}}}\"";

            Payload serializedItemData = new();
            serializedItemData.Metadata.Add(PayloadConverter.PayloadMetadataEncodingKey, PayloadMetadataEncodingValueBytes);
            serializedItemData.Data = ByteString.CopyFromUtf8(itemInfo);

            SerializationUtil.Add(serializedDataAccumulator, serializedItemData);
            return true;
        }
    }
}
