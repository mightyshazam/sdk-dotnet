using System;
using System.Text;
using Temporal.Util;
using Google.Protobuf;
using Temporal.Api.Common.V1;
using System.Diagnostics;

namespace Temporal.Serialization
{
    /// <summary>
    /// <para>
    /// This is a sample. In the long run, we will move it to the samples directory.
    /// For now, we keep it here to exemplify custom <c>IPayloadConverter</c> implementation,
    /// and to provide a useful debugging tool.
    /// </para>
    /// This payload converter is mainly intended for debugging and development.
    /// It does not actually convert the data.
    /// Instead: On the outgoing path it creates a payload that contains a UTF8 string with some metadata about what was going to be converted
    /// In the incoming path, it contains a string description about the incoming data and tried to convert it to the requested type.
    /// If tat is not possible, the default for the requested type is returned.
    /// Either way, the metadata / description is traced.
    /// </summary>    
    public class DiagnosticCatchAllPayloadConverter : IPayloadConverter
    {
        public const string PayloadMetadataEncodingValue = "json/plain";
        //public const string PayloadMetadataEncodingValue = "binary/plain";

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

            string msgStr = msg.ToString();
            Trace.WriteLine($"{this.GetType().Name}.{nameof(TryDeserialize)}<{typeof(T).Name}>(..): \n{msgStr}");

            if (!msgStr.TryCast<string, T>(out item))
            {
                item = default(T);
            }

            return true;
        }

        public bool TrySerialize<T>(T item, Payloads serializedDataAccumulator)
        {
            string itemString = (item == null) ? "null" : item.ToString();
            string itemInfo = $"\"[{item.TypeOf()}]{{{itemString}}}\"";

            Trace.WriteLine($"{this.GetType().Name}.{nameof(TrySerialize)}<{typeof(T).Name}>(..): \n{itemInfo}");

            Payload serializedItemData = new();
            serializedItemData.Metadata.Add(PayloadConverter.PayloadMetadataEncodingKey, PayloadMetadataEncodingValueBytes);
            serializedItemData.Data = ByteString.CopyFromUtf8(itemInfo);

            SerializationUtil.Add(serializedDataAccumulator, serializedItemData);
            return true;
        }

        public override bool Equals(object obj)
        {
            return Object.ReferenceEquals(this, obj)
                        || ((obj != null) && this.GetType().Equals(obj.GetType()));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
