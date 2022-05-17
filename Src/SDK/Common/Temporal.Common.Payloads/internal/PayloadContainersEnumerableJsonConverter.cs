using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Temporal.Common.Payloads
{
    public class PayloadContainersEnumerableJsonConverter : JsonConverter<PayloadContainers.Enumerable>
    {
        public override void WriteJson(JsonWriter writer, PayloadContainers.Enumerable value, JsonSerializer serializer)
        {
            if (value.BackingEnumerable is JObject jo)
            {
                jo.WriteTo(writer);
                return;
            }
            writer.WriteStartArray();
            writer.WriteStartObject();
            foreach (object entry in value.BackingEnumerable)
            {
                jo = entry as JObject ?? JObject.FromObject(entry);
                jo.WriteTo(writer);
            }

            writer.WriteEndObject();
            writer.WriteEndArray();
        }

        public override PayloadContainers.Enumerable ReadJson(
            JsonReader reader,
            Type objectType,
            PayloadContainers.Enumerable existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            JObject o = JObject.Load(reader);
            return new PayloadContainers.Enumerable(o.ToObject<IList<object>>());
        }
    }

    public class PayloadContainersEnumerableJsonConverter<T> : JsonConverter<PayloadContainers.Enumerable<T>>
    {
        public override void WriteJson(JsonWriter writer, PayloadContainers.Enumerable<T> value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            foreach (T entry in value)
            {
                JToken jo = JToken.FromObject(entry);
                jo.WriteTo(writer);
            }

            writer.WriteEndArray();
        }

        public override PayloadContainers.Enumerable<T> ReadJson(
            JsonReader reader,
            Type objectType,
            PayloadContainers.Enumerable<T> existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            JObject o = JObject.Load(reader);
            return new PayloadContainers.Enumerable<T>(o.ToObject<IList<T>>());
        }
    }
}