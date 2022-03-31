using System;
using System.Collections.Generic;
using Candidly.Util;

using Temporal.Common.Payloads;

using SerializedPayload = Temporal.Api.Common.V1.Payload;
using SerializedPayloads = Temporal.Api.Common.V1.Payloads;

namespace Temporal.Serialization
{
    public sealed class DefaultDataConverter : IDataConverter, IPayloadConverter
    {
        public static class ExceptionTags
        {
            public const string IsCannotSerializeEnumerable = "IsCannotSerializeEnumerable";
        }

        public static IList<IPayloadConverter> CreateDefaultConverters()
        {
            List<IPayloadConverter> converters = new List<IPayloadConverter>(capacity: 1)
            {
                new VoidPayloadConverter(),
                new NullPayloadConverter(),
                new CatchAllPayloadConverter()
                //@ToDo
            };

            return converters;
        }

        public static IList<IPayloadCodec> CreateDefaultCodecs()
        {
            List<IPayloadCodec> converters = new List<IPayloadCodec>(capacity: 0)
            {
                // No default codecs
            };

            return converters;
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
            _converters = EnsureIsList(converters);
            _codecs = EnsureIsList(codecs);
        }

        public IReadOnlyList<IPayloadConverter> PayloadConverters { get { return _converters; } }

        public IReadOnlyList<IPayloadCodec> PayloadCodecs { get { return _codecs; } }

        public SerializedPayloads Serialize<T>(T item)
        {
            SerializedPayloads serializedData = new();

            if ((item != null && item is System.Collections.IEnumerable)
                    || (typeof(System.Collections.IEnumerable).IsAssignableFrom(typeof(T))))
            {
                throw CreateCannotUseEnumerableArgumentException();
            }
            if (item != null && item is IUnnamedValuesContainer itemsContainer)
            {
                for (int i = 0; i < itemsContainer.Count; i++)
                {
                    ConvertToPayload(itemsContainer.GetValue<object>(i), serializedData);
                }
            }
            else
            {
                ConvertToPayload<T>(item, serializedData);
            }

            serializedData = Encode(serializedData);

            return serializedData;
        }

        public T Deserialize<T>(SerializedPayloads serializedData)
        {
            Validate.NotNull(serializedData);

            serializedData = Decode(serializedData);

            if (serializedData.Payloads_.Count > 1)
            {
                // If we have multiple payloads, they MUST be deserialized into a container that allows strictly typed lazy deserialization.
                // That will be used by SDK to offer APIs that access data when needed.

                if (typeof(PayloadContainers.ForUnnamedValues.SerializedDataBacked).IsAssignableFrom(typeof(T)))
                {
                    PayloadContainers.ForUnnamedValues.SerializedDataBacked container = new(serializedData.Payloads_, this);
                    return container.Cast<PayloadContainers.ForUnnamedValues.SerializedDataBacked, T>();
                }
                else
                {
                    throw new ArgumentException($"Cannot {nameof(Deserialize)} an item of type \"{typeof(T).FullName}\""
                                              + $" because the specified {nameof(serializedData)} contains {serializedData.Payloads_.Count}"
                                              + $" {nameof(Temporal.Api.Common.V1.Payload)} items"
                                              + $" ({nameof(serializedData)} with multiple {nameof(Temporal.Api.Common.V1.Payload)}-items"
                                              + $" may only be dezerialized into a container of type"
                                              + $" \"{typeof(PayloadContainers.ForUnnamedValues.SerializedDataBacked).FullName}\").");
                }
            }
            else if (serializedData.Payloads_.Count == 1)
            {
                T deserializedItem = ConvertFromPayload<T>(serializedData.Payloads_[0]);
                return deserializedItem;
            }
            else if (serializedData.Payloads_.Count == 0)
            {
                T deserializedItem = ConvertFromPayload<T>(null);
                return deserializedItem;
            }

            throw new ArgumentException($"Cannot {nameof(Deserialize)} the specified {nameof(serializedData)} becasue it"
                                      + $" contains {serializedData.Payloads_.Count} {nameof(Temporal.Api.Common.V1.Payload)} items.");
        }

        private void ConvertToPayload<T>(T item, SerializedPayloads serializedData)
        {
            if (((IPayloadConverter) this).TrySerialize(item, out SerializedPayload itemData))
            {
                if (itemData != null)
                {
                    serializedData.Payloads_.Add(itemData);
                }

                return;
            }

            throw new InvalidOperationException($"Cannot {nameof(Serialize)} the specified item of type \"{item.TypeOf()}\""
                                              + $" because none of the {_converters.Count} {nameof(PayloadConverters)} inside"
                                              + $" of this {nameof(DefaultDataConverter)} instance can convert this item.");
        }

        bool IPayloadConverter.TrySerialize<T>(T item, out SerializedPayload serializedData)
        {
            for (int c = 0; c < _converters.Count; c++)
            {
                if (_converters[c] != null && _converters[c].TrySerialize(item, out serializedData))
                {
                    return true;
                }
            }

            serializedData = null;
            return false;
        }

        private T ConvertFromPayload<T>(SerializedPayload data)
        {
            if (((IPayloadConverter) this).TryDeserialize(data, out T item))
            {
                return item;
            }

            throw new InvalidOperationException($"Cannot {nameof(Deserialize)} an item of type \"{typeof(T).FullName}\""
                                              + $" because none of the {_converters.Count} {nameof(PayloadConverters)} inside"
                                              + $" of this {nameof(DefaultDataConverter)} instance can convert this item.");
        }

        bool IPayloadConverter.TryDeserialize<T>(SerializedPayload serializedItemData, out T item)
        {
            Validate.NotNull(serializedItemData);

            // Only the fist converter that CAN serialize gets applied. So must traverse in the same order.
            // Given the small number of converter kinds this is more efficient than a lookup.
            for (int c = 0; c < _converters.Count; c++)
            {
                if (_converters[c] != null && _converters[c].TryDeserialize<T>(serializedItemData, out item))
                {
                    return true;
                }
            }

            item = default(T);
            return false;
        }

        private SerializedPayloads Encode(SerializedPayloads data)
        {
            for (int c = 0; c < _codecs.Count; c++)
            {
                if (_codecs[c] != null)
                {
                    data = _codecs[c].Encode(data);
                }
            }

            return data;
        }

        private SerializedPayloads Decode(SerializedPayloads data)
        {
            // Since all codecs apply, must traverse in the reverse order of encoding.
            for (int c = _codecs.Count - 1; c >= 0; c--)
            {
                data = _codecs[c].Decode(data);
            }

            return data;
        }

        private static List<T> EnsureIsList<T>(IEnumerable<T> items)
        {
            if (items == null)
            {
                return new List<T>(capacity: 0);
            }

            if (items is List<T> itemsList)
            {
                return itemsList;
            }

            List<T> newList = (items is IReadOnlyCollection<T> itemsCollection)
                                    ? new List<T>(capacity: itemsCollection.Count)
                                    : new List<T>();

            foreach (T item in items)
            {
                newList.Add(item);
            }

            return newList;
        }

        private static ArgumentException CreateCannotUseEnumerableArgumentException()
        {
            ArgumentException argEx = new($"The specified data item is an IEnumerable. Specifying Enumerables (arrays,"
                                        + $" collections, ...) for workflow payloads is not permitted because it is ambiguous"
                                        + $" whether you intended to use the Enumerable as the single argument/payload, or"
                                        + $" whether you intended to use multiple arguments/payloads, one for each element of"
                                        + $" your collection. To use an Enumerable as a single argument/payload, or to use several"
                                        + $" arguments/payloads, you need to wrap your data into an {nameof(Temporal.Common.IPayload)}"
                                        + $" container. For example, to use an array of integers (`int[] data`) as a SINGLE argument,"
                                        + $" you can wrap it like this: `SomeWorkflowApi(.., {nameof(Temporal.Common.Payload)}"
                                        + $".{nameof(Temporal.Common.Payload.Unnamed)}<int[]>(data))`."
                                        + $" To use the contents of `data` as MULTIPLE integer arguments, you can wrap it like this:"
                                        + $" `SomeWorkflowApi(.., {nameof(Temporal.Common.Payload)}"
                                        + $".{nameof(Temporal.Common.Payload.Unnamed)}<int>(data))`."
                                        + $" Also, note that, if suported by the workflow implementation, it is better to use a"
                                        + $" {nameof(Temporal.Common.Payload.Named)} {nameof(Temporal.Common.Payload)}-container.");

            argEx.Data.Add(ExceptionTags.IsCannotSerializeEnumerable, true);
            return argEx;
        }
    }
}
