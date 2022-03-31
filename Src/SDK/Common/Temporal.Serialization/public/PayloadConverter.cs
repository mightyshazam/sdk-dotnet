using Google.Protobuf;

namespace Temporal.Serialization
{
    public static class PayloadConverter
    {
        public const string PayloadMetadataEncodingKey = "encoding";

        public static ByteString GetOrCreateBytes(string value, ref ByteString valueBytes)
        {
            ByteString bytes = valueBytes;
            if (bytes == null)
            {
                bytes = ByteString.CopyFromUtf8(value);
                valueBytes = bytes;
            }

            return bytes;
        }
    }
}
