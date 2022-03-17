using System;
using System.Collections.Generic;
using Temporal.Common.DataModel;

namespace Temporal.Serialization
{
    public class SerializationApi
    {
    }

    public interface IPayloadConverter
    {
        bool TryDeserialize<T>(PayloadsCollection serializedData, out T item);
        bool TrySerialize<T>(T item, out PayloadsCollection serializedData);
    }

    public interface IPayloadCodec
    {
        PayloadsCollection Decode(PayloadsCollection data);
        PayloadsCollection Encode(PayloadsCollection data);        
    }

    public interface IDataConverter
    {
        T Deserialize<T>(PayloadsCollection serializedData);
        PayloadsCollection Serialize<T>(T item);        
        PayloadsCollection Serialize<T>(params T[] items);
    }

    public sealed class DefaultDataConverter : IDataConverter
    {
        public static IList<IPayloadConverter> CreateDefaultConverters()
        {
            return null;
        }

        public static IList<IPayloadCodec> CreateDefaultCodecs()
        {
            return null;
        }

        private readonly List<IPayloadConverter> _converters;
        private readonly List<IPayloadCodec> _codecs;

        public DefaultDataConverter()
            : this(CreateDefaultConverters(), CreateDefaultCodecs())
        {
        }

        public DefaultDataConverter(IEnumerable<IPayloadCodec> codecs)
            : this(CreateDefaultConverters(), codecs)
        {
        }

        public DefaultDataConverter(IEnumerable<IPayloadConverter> converters)
            : this(converters, CreateDefaultCodecs())
        {

        }

        public DefaultDataConverter(IEnumerable<IPayloadConverter> converters, IEnumerable<IPayloadCodec> codecs)
        {
            if (converters == null)
            {
                _converters = new List<IPayloadConverter>(capacity: 0);
            }
            else
            {
                if (converters is IReadOnlyCollection<IPayloadConverter> converterCollection)
                {
                    _converters = new List<IPayloadConverter>(capacity: converterCollection.Count);
                }
                else
                {
                    _converters = new List<IPayloadConverter>();
                }

                foreach(IPayloadConverter converter in converters)
                {
                    if (converter != null)
                    {
                        _converters.Add(converter);
                    }
                }
            }

            if (codecs == null)
            {
                _codecs = new List<IPayloadCodec>(capacity: 0);
            }
            else
            {
                if (codecs is IReadOnlyCollection<IPayloadCodec> codecCollection)
                {
                    _codecs = new List<IPayloadCodec>(capacity: codecCollection.Count);
                }
                else
                {
                    _codecs = new List<IPayloadCodec>();
                }

                foreach (IPayloadCodec codec in codecs)
                {
                    if (codec != null)
                    {
                        _codecs.Add(codec);
                    }
                }
            }
        }

        public IReadOnlyList<IPayloadConverter> PayloadConverters { get { return _converters; } }

        public IReadOnlyList<IPayloadCodec> PayloadCodecs { get { return _codecs; } }

        public PayloadsCollection Serialize<T>(T item)
        {
            PayloadsCollection data = ConvertToPayload<T>(item);
            data = Encode(data);

            return data;
        }

        public PayloadsCollection Serialize<T>(params T[] items)
        {
            PayloadsCollection data;

            if (items == null || items.Length == 0)
            {
                data = PayloadsCollection.Empty;
            }
            else
            {
                MutablePayloadsCollection arrayData = new();

                for (int i = 0; i < items.Length; i++)
                {
                    PayloadsCollection itemData = ConvertToPayload<T>(items[i]);
                    for (int d = 0; d < itemData.Count; d++)
                    {
                        arrayData.Add(itemData[d]);
                    }
                }

                data = arrayData;
            }

            data = Encode(data);

            return data;
        }

        public T Deserialize<T>(PayloadsCollection serializedData)
        {
            PayloadsCollection data = Decode(serializedData);
            T deserializedItem = ConvertFromPayload<T>(data);

            return deserializedItem;
        }

        private PayloadsCollection ConvertToPayload<T>(T item)
        {
            for (int c = 0; c < _converters.Count; c++)
            {
                if (_converters[c].TrySerialize(item, out PayloadsCollection data))
                {
                    return data;
                }
            }

            throw new InvalidOperationException($"This {nameof(DefaultDataConverter)} cannot {nameof(Serialize)} the specified"
                                              + $" item of type {typeof(T).FullName} because none of the {_converters.Count}"
                                              + $" {nameof(PayloadConverters)} inside of this {nameof(DefaultDataConverter)}"
                                              + $" instance can convert this item.");
        }

        private T ConvertFromPayload<T>(PayloadsCollection data)
        {
            // Only the fist converter that CAN serialize gets applied. So must traverse in the same order.
            // Given the small number of converter kinds this is more efficient than a lookup.
            for (int c = 0; c < _converters.Count; c++)
            {
                if (_converters[c].TryDeserialize<T>(data, out T item))
                {
                    return item;
                }
            }

            throw new InvalidOperationException($"This {nameof(DefaultDataConverter)} cannot {nameof(Serialize)} the specified"
                                              + $" item of type {typeof(T).FullName} because none of the {_converters.Count}"
                                              + $" {nameof(PayloadConverters)} inside of this {nameof(DefaultDataConverter)}"
                                              + $" instance can convert this item.");
        }

        private PayloadsCollection Encode(PayloadsCollection data)
        {
            for (int c = 0; c < _codecs.Count; c++)
            {
                data = _codecs[c].Encode(data);
            }

            return data;
        }

        private PayloadsCollection Decode(PayloadsCollection data)
        {
            // Since all codecs apply, must traverse in the reverse order of encoding.
            for (int c = _codecs.Count - 1; c >= 0; c--)
            {
                data = _codecs[c].Decode(data);
            }

            return data;
        }
    }
}
