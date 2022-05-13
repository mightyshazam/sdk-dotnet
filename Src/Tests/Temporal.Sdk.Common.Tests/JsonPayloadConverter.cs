using System;
using System.Text.Json;
using Google.Protobuf;
using Temporal.Serialization;
using Payload = Temporal.Api.Common.V1.Payload;

namespace Temporal.Sdk.Common.Tests
{
    internal class JsonPayloadConverter : IPayloadConverter
    {
        public bool TryDeserialize<T>(Api.Common.V1.Payloads serializedData, out T item)
        {
            item = JsonSerializer.Deserialize<T>(serializedData.Payloads_[0].Data.Span, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
            return true;
        }

        public bool TrySerialize<T>(T item, Api.Common.V1.Payloads serializedDataAccumulator)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            serializedDataAccumulator.Payloads_.Add(
                new Payload
                {
                    Data = ByteString.CopyFrom(JsonSerializer.SerializeToUtf8Bytes(item)),
                });

            return true;
        }
    }
}