using System;
using System.Collections;
using System.Collections.Generic;
using Candidly.Util;

using SerializedPayloads = Temporal.Api.Common.V1.Payloads;

namespace Temporal.Serialization
{
    public sealed class CompositePayloadConverter : IPayloadConverter, IEnumerable<IPayloadConverter>, IDisposable
    {
        public static IList<IPayloadConverter> CreateDefaultConverters()
        {
            List<IPayloadConverter> converters = new List<IPayloadConverter>(capacity: 4)
            {
                new VoidPayloadConverter(),
                new NullPayloadConverter(),
                new UnnamedValuesContainerPayloadConverter(),
                new CatchAllPayloadConverter(),
                //@ToDo
            };

            return converters;
        }

        private readonly List<IPayloadConverter> _converters;

        public CompositePayloadConverter()
            : this(CreateDefaultConverters())
        {
        }

        public CompositePayloadConverter(IEnumerable<IPayloadConverter> converters)
        {
            _converters = SerializationUtil.EnsureIsList(converters);

            for (int c = 0; c < _converters.Count; c++)
            {
                if (_converters[c] is DelegatingPayloadConverterBase delegatingConverter)
                {
                    delegatingConverter.InitDelegates(this);
                }
            }
        }

        public IReadOnlyList<IPayloadConverter> Converters { get { return _converters; } }

        public bool TrySerialize<T>(T item, SerializedPayloads serializedDataAccumulator)
        {
            Validate.NotNull(serializedDataAccumulator);

            for (int c = 0; c < _converters.Count; c++)
            {
                if (_converters[c] != null && _converters[c].TrySerialize(item, serializedDataAccumulator))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryDeserialize<T>(SerializedPayloads serializedData, out T deserializedItem)
        {
            Validate.NotNull(serializedData);

            // Only the first converter that CAN serialize got applied during serialization. So must traverse in the same order.
            // Given the small number of converter kinds this is more efficient than a lookup.
            for (int c = 0; c < _converters.Count; c++)
            {
                if (_converters[c] != null && _converters[c].TryDeserialize<T>(serializedData, out deserializedItem))
                {
                    return true;
                }
            }

            deserializedItem = default(T);
            return false;
        }

        public void Dispose()
        {
            while (_converters.Count > 0)
            {
                IPayloadConverter converter = _converters[_converters.Count - 1];
                _converters.RemoveAt(_converters.Count - 1);

                if (converter != null && converter is IDisposable disposableConverter)
                {
                    disposableConverter.Dispose();
                }
            }
        }

        IEnumerator<IPayloadConverter> IEnumerable<IPayloadConverter>.GetEnumerator()
        {
            return Converters.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<IPayloadConverter>) this).GetEnumerator();
        }
    }
}
