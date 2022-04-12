using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Candidly.Util;

using Temporal.Common.Payloads;

using SerializedPayload = Temporal.Api.Common.V1.Payload;
using SerializedPayloads = Temporal.Api.Common.V1.Payloads;

namespace Temporal.Serialization
{
    public sealed class AggregatePayloadConverter : IPayloadConverter
    {
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

        private readonly List<IPayloadConverter> _converters;

        public AggregatePayloadConverter()
            : this(CreateDefaultConverters())
        {
        }

        public AggregatePayloadConverter(IEnumerable<IPayloadConverter> converters)
        {
            _converters = SerializationUtil.EnsureIsList(converters);
        }

        public IReadOnlyList<IPayloadConverter> Converters { get { return _converters; } }

        public bool TrySerialize<T>(T item, SerializedPayloads serializedDataAccumulator)
        {
            Validate.NotNull(serializedDataAccumulator);

            if (item != null && item is IUnnamedValuesContainer itemsContainer)
            {
                // If item is an IUnnamedValuesContainer, handle it directly:
                // delegate each contained unnamed value separately to the contained converters:

                for (int i = 0; i < itemsContainer.Count; i++)
                {
                    try
                    {
                        PayloadConverter.Serialize(this, itemsContainer.GetValue<object>(i), serializedDataAccumulator);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Error serializing value at index {i} of the specified container"
                                                          + $" of type \"{itemsContainer.GetType().FullName}\".",
                                                            ex);
                    }
                }

                return true;
            }
            else
            {
                // For all items are delegsted to the contained converters:

                return TrySerializeByFirstConverterMatch<T>(item, serializedDataAccumulator);
            }
        }

        public bool TryDeserialize<T>(SerializedPayloads serializedData, out T deserializedItem)
        {
            Validate.NotNull(serializedData);

            // `PayloadContainers.ForUnnamedValues.SerializedDataBacked` is a container that supports strictly typed
            // lazy deserialization of data when the value is actually requested.
            // It supports multiple `Payload`-entries within the `Payloads`-collection.
            // That container is be used by SDK to offer APIs that access data when needed.

            if (typeof(PayloadContainers.ForUnnamedValues.SerializedDataBacked).IsAssignableFrom(typeof(T)))
            {
                PayloadContainers.ForUnnamedValues.SerializedDataBacked container = new(serializedData, this);
                deserializedItem = container.Cast<PayloadContainers.ForUnnamedValues.SerializedDataBacked, T>();
                return true;
            }

            return TryDeserializeByFirstConverterMatch<T>(serializedData, out deserializedItem);
        }

        private bool TrySerializeByFirstConverterMatch<T>(T item, SerializedPayloads serializedDataAccumulator)
        {
            for (int c = 0; c < _converters.Count; c++)
            {
                if (_converters[c] != null && _converters[c].TrySerialize(item, serializedDataAccumulator))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryDeserializeByFirstConverterMatch<T>(SerializedPayloads serializedData, out T item)
        {
            // Only the first converter that CAN serialize got applied during serialization. So must traverse in the same order.
            // Given the small number of converter kinds this is more efficient than a lookup.
            for (int c = 0; c < _converters.Count; c++)
            {
                if (_converters[c] != null && _converters[c].TryDeserialize<T>(serializedData, out item))
                {
                    return true;
                }
            }

            item = default(T);
            return false;
        }
    }
}
