using System;
using System.IO;
using Google.Protobuf;
using Temporal.Api.Common.V1;
using Temporal.Util;

namespace Temporal.Serialization
{
    public class RawMemoryPayloadConverter : IPayloadConverter
    {
        public const string PayloadMetadataEncodingValue = "binary/plain";

        private static ByteString s_payloadMetadataEncodingValueBytes = null;

        private static ByteString PayloadMetadataEncodingValueBytes
        {
            get { return PayloadConverter.GetOrCreateBytes(PayloadMetadataEncodingValue, ref s_payloadMetadataEncodingValueBytes); }
        }

        public bool TryDeserialize<T>(Payloads serializedData, out T item)
        {
            if (SerializationUtil.TryGetSinglePayload(serializedData, out Payload serializedItem)
                    && serializedItem.Metadata.TryGetValue(PayloadConverter.PayloadMetadataEncodingKey, out ByteString encodingBytes)
                    && PayloadMetadataEncodingValueBytes.Equals(encodingBytes))
            {
                ByteString serializedBytes = serializedItem.Data;

                // Handle the case `typeof(T) == typeof(ByteString)`:
                if (serializedBytes is T byteStringItem)
                {
                    item = byteStringItem;
                    return true;
                }

#if NETCOREAPP3_1_OR_GREATER
                // Handle the case `typeof(T) == typeof(ReadOnlyMemory<byte>)`:
                if (serializedBytes.Memory is T readOnlyMemoryItem)
                {
                    item = readOnlyMemoryItem;
                    return true;
                }

                if (typeof(Memory<byte>) == typeof(T))
                {
                    Memory<byte> memoryItem = new(serializedBytes.ToByteArray());
                    item = memoryItem.Cast<Memory<byte>, T>();
                    return true;
                }
#endif

                if (typeof(Stream) == typeof(T) || typeof(MemoryStream) == typeof(T))
                {
                    byte[] bytes = serializedBytes.ToByteArray();
                    MemoryStream memoryStreamItem = new(bytes, 0, bytes.Length, writable: true, publiclyVisible: true);
                    memoryStreamItem.Position = 0;
                    item = memoryStreamItem.Cast<MemoryStream, T>();
                    return true;
                }
            }

            item = default(T);
            return false;
        }

        public bool TrySerialize<T>(T item, Payloads serializedDataAccumulator)
        {
            if (item == null)
            {
                return false;
            }

            switch (item)
            {
                case ByteString byteString:
                    return TrySerialize(byteString, serializedDataAccumulator);

#if NETCOREAPP3_1_OR_GREATER
                case ReadOnlyMemory<byte> byteMem:
                    return TrySerialize(ByteString.CopyFrom(byteMem.Span), serializedDataAccumulator);

                case Memory<byte> byteMem:
                    return TrySerialize(ByteString.CopyFrom(byteMem.Span), serializedDataAccumulator);
#endif

                case MemoryStream memStream:
                    if (memStream.TryGetBuffer(out ArraySegment<byte> memStreamBuff))
                    {
                        return TrySerialize(ByteString.CopyFrom(memStreamBuff.Array, memStreamBuff.Offset, memStreamBuff.Count),
                                            serializedDataAccumulator);
                    }
                    else
                    {
                        return TrySerialize(ByteString.FromStream(memStream), serializedDataAccumulator);
                    }

                case Stream stream:
                    return TrySerialize(ByteString.FromStream(stream), serializedDataAccumulator);

                default:
                    return false;
            }
        }

        private bool TrySerialize(ByteString data, Payloads serializedDataAccumulator)
        {
            if (data != null)
            {
                Payload serializedItemData = new();
                serializedItemData.Metadata.Add(PayloadConverter.PayloadMetadataEncodingKey, PayloadMetadataEncodingValueBytes);
                serializedItemData.Data = data;

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
