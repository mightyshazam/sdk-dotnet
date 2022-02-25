using System;
using Temporal.Common.DataModel;

namespace Temporal.Serialization
{
    public class SerializationApi
    {
    }


    public interface IPayloadSerializer
    {
        T Deserialize<T>(PayloadsCollection payloadsToParse);
        PayloadsCollection Serialize<T>(T item);
    }

    public class JsonPayloadSerializer : IPayloadSerializer
    {
        public T Deserialize<T>(PayloadsCollection payloadsToParse) { return default(T); }

        public PayloadsCollection Serialize<T>(T item) { return null; }
    }

}
