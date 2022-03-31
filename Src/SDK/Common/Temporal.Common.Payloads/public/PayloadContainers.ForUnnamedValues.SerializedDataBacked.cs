using System;
using System.Collections;
using System.Collections.Generic;
using Candidly.Util;
using Temporal.Serialization;

using SerializedPayload = Temporal.Api.Common.V1.Payload;

namespace Temporal.Common.Payloads
{
    public static partial class PayloadContainers
    {
        /// <summary>
        /// <c>IUnnamedValuesContainer</c> implementation backed by raw serialized data (rather than by actual values).
        /// </summary>
        public static partial class ForUnnamedValues
        {
            public class SerializedDataBacked : IUnnamedValuesContainer, IPayload
            {
                private readonly IReadOnlyList<SerializedPayload> _serializedData;
                private readonly IPayloadConverter _individualPayloadDeserializer;

                public SerializedDataBacked(IReadOnlyList<SerializedPayload> serializedData,
                                                IPayloadConverter individualPayloadDeserializer)
                {
                    Validate.NotNull(serializedData);
                    Validate.NotNull(individualPayloadDeserializer);

                    _serializedData = serializedData;
                    _individualPayloadDeserializer = individualPayloadDeserializer;
                }

                public int Count
                {
                    get { return _serializedData.Count; }
                }

                public TVal GetValue<TVal>(int index)
                {
                    if (index >= 0 && index < Count)
                    {
                        // @ToDo: Cache data that is already deserialized (pay attention to TVal and index matching)

                        if (_individualPayloadDeserializer.TryDeserialize<TVal>(_serializedData[index], out TVal value))
                        {
                            return value;
                        }

                        throw new InvalidOperationException($"Cannot {nameof(GetValue)} of type {nameof(TVal)}=\"{typeof(TVal).FullName}\""
                                                          + $" because the {nameof(_individualPayloadDeserializer)} of type"
                                                          + $" \"{_individualPayloadDeserializer.GetType().FullName}\" that backs this"
                                                          + $" instance of \"{this.GetType().FullName}\" cannot deserialize the payload"
                                                          + $" as requred.");
                    }

                    throw CreateNoSuchIndexException(index, Count);
                }

                public bool TryGetValue<TVal>(int index, out TVal value)
                {
                    // @ToDo: Cache data that is already deserialized (pay attention to TVal and index matching)

                    if (index >= 0 && index < Count)
                    {
                        return _individualPayloadDeserializer.TryDeserialize<TVal>(_serializedData[index], out value);
                    }

                    throw CreateNoSuchIndexException(index, Count);
                }

                public IEnumerable<IUnnamedValuesContainerEntry> Values
                {
                    get
                    {
                        throw new NotImplementedException("@ToDo");
                    }
                }

                public IEnumerator<IUnnamedValuesContainerEntry> GetEnumerator()
                {
                    throw new NotImplementedException("@ToDo");
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return this.GetEnumerator();
                }

                public IUnnamedValuesContainerEntry this[int index]
                {
                    get
                    {
                        throw new NotImplementedException("@ToDo");
                    }
                }

                private static ArgumentException CreateNoSuchIndexException(int index, int containerItemCount)
                {
                    if (index < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index), $"The value of {nameof(index)} may not be negative,"
                                                                           + $" but `{index}` was specified.");
                    }

                    if (index >= containerItemCount)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index),
                                                              $"This {nameof(IUnnamedValuesContainer)} includes"
                                                            + $" {containerItemCount} items, but the {nameof(index)}=`{index}` was specified.");
                    }

                    return new ArgumentException(message: $"Invalid value of {nameof(index)}: {index}.", paramName: nameof(index));
                }
            }
        }
    }
}
