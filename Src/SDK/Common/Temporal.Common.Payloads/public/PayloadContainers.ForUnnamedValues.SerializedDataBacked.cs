using System;
using System.Collections;
using System.Collections.Generic;
using Candidly.Util;
using Temporal.Serialization;

using SerializedPayload = Temporal.Api.Common.V1.Payload;
using SerializedPayloads = Temporal.Api.Common.V1.Payloads;

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
                private readonly SerializedPayloads _serializedData;
                private readonly int _countPayloadEntries;
                private readonly IPayloadConverter _payloadConverter;

                private readonly KeyValuePair<Type, object>[] _cache;

                public SerializedDataBacked(SerializedPayloads serializedData,
                                            IPayloadConverter payloadConverter)
                {
                    Validate.NotNull(serializedData);
                    Validate.NotNull(payloadConverter);

                    _serializedData = serializedData;
                    _countPayloadEntries = SerializationUtil.GetPayloadCount(serializedData);
                    _payloadConverter = payloadConverter;

                    _cache = new KeyValuePair<Type, object>[_countPayloadEntries];
                    for (int i = 0; i < _countPayloadEntries; i++)
                    {
                        _cache[i] = new KeyValuePair<Type, object>(null, null);
                    }
                }

                public int Count
                {
                    get { return _countPayloadEntries; }
                }

                public TVal GetValue<TVal>(int index)
                {
                    if (index >= 0 && index < Count)
                    {
                        if (TryGetValueFromCache<TVal>(index, out TVal value))
                        {
                            return value;
                        }

                        try
                        {
                            value = _payloadConverter.Deserialize<TVal>(GetIndexPayload(index));
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException($"Cannot {nameof(GetValue)} of type"
                                                              + $" {nameof(TVal)}=\"{typeof(TVal).FullName}\""
                                                              + $" because the {nameof(_payloadConverter)} of type"
                                                              + $" \"{_payloadConverter.GetType().FullName}\" that backs this"
                                                              + $" instance of \"{this.GetType().FullName}\" cannot deserialize the payload"
                                                              + $" as requred.",
                                                                ex);
                        }

                        return GetOrUpdateCache(index, value);
                    }

                    throw CreateNoSuchIndexException(index, Count);
                }

                public bool TryGetValue<TVal>(int index, out TVal value)
                {
                    if (index >= 0 && index < Count)
                    {
                        if (TryGetValueFromCache<TVal>(index, out value))
                        {
                            return true;
                        }

                        bool canDeserialize;
                        try
                        {
                            canDeserialize = _payloadConverter.TryDeserialize<TVal>(GetIndexPayload(index), out value);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException($"Cannot {nameof(TryGetValue)} of type"
                                                              + $" {nameof(TVal)}=\"{typeof(TVal).FullName}\""
                                                              + $" because the {nameof(_payloadConverter)} of type"
                                                              + $" \"{_payloadConverter.GetType().FullName}\" that backs this"
                                                              + $" instance of \"{this.GetType().FullName}\" threw an error while"
                                                              + $" deserializing the payoad as requred.",
                                                                ex);
                        }

                        if (canDeserialize)
                        {
                            value = GetOrUpdateCache(index, value);
                        }

                        return canDeserialize;
                    }

                    throw CreateNoSuchIndexException(index, Count);
                }

                public IEnumerable<IUnnamedValuesContainerEntry> Values
                {
                    get
                    {
                        for (int v = 0; v < Count; v++)
                        {
                            yield return new UnnamedValuesContainerEntry<object>(v, this);
                        }
                    }
                }

                public IEnumerator<IUnnamedValuesContainerEntry> GetEnumerator()
                {
                    return new UnnamedValuesContainerEnumerator(this);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return this.GetEnumerator();
                }

                public IUnnamedValuesContainerEntry this[int index]
                {
                    get
                    {
                        if (index >= 0 && index < Count)
                        {
                            return new UnnamedValuesContainerEntry<object>(index, this);
                        }

                        throw CreateNoSuchIndexException(index, Count);
                    }
                }

                private bool TryGetValueFromCache<TVal>(int index, out TVal value)
                {
                    if (typeof(TVal) == _cache[index].Key)
                    {
                        lock (_cache)
                        {
                            if (typeof(TVal) == _cache[index].Key)
                            {
                                value = _cache[index].Value.Cast<object, TVal>();
                                return true;
                            }
                        }
                    }

                    value = default(TVal);
                    return false;
                }

                private TVal GetOrUpdateCache<TVal>(int index, TVal value)
                {
                    lock (_cache)
                    {
                        if (typeof(TVal) == _cache[index].Key)
                        {
                            return _cache[index].Value.Cast<object, TVal>();
                        }

                        _cache[index] = new KeyValuePair<Type, object>(typeof(TVal), (object) value);
                        return value;
                    }
                }

                private SerializedPayloads GetIndexPayload(int index)
                {
                    if (Count <= 1)
                    {
                        return _serializedData;
                    }

                    SerializedPayloads wrapper = new();
                    wrapper.Payloads_.Add(_serializedData.Payloads_[index]);
                    return wrapper;
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
