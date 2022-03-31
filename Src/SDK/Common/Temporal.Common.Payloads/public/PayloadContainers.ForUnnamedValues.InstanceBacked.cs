using System;
using System.Collections;
using System.Collections.Generic;
using Candidly.Util;

namespace Temporal.Common.Payloads
{
    public static partial class PayloadContainers
    {
        /// <summary>
        /// <c>IUnnamedValuesContainer</c> implementation backed by actual values (rather than a raw payload).
        /// </summary>
        public static partial class ForUnnamedValues
        {
            public struct InstanceBacked<T> : IUnnamedValuesContainer, IPayload
            {
                private readonly IReadOnlyList<T> _values;

                public InstanceBacked(IReadOnlyList<T> values)
                {
                    Validate.NotNull(values);

                    _values = values;
                }

                public int Count
                {
                    get { return _values.Count; }
                }

                public TVal GetValue<TVal>(int index)
                {
                    if (index >= 0 && index < Count)
                    {
                        return _values[index].Cast<T, TVal>();
                    }

                    throw CreateNoSuchIndexException(index, Count);
                }

                public bool TryGetValue<TVal>(int index, out TVal value)
                {
                    if (index >= 0 && index < Count)
                    {
                        return _values[index].TryCast<T, TVal>(out value);
                    }

                    throw CreateNoSuchIndexException(index, Count);
                }

                public Type GetValueType(int index)
                {
                    if (index >= 0 && index < Count)
                    {
                        return _values[index].TypeOf();
                    }

                    throw CreateNoSuchIndexException(index, Count);
                }

                public IEnumerable<IUnnamedValuesContainerEntry> Values
                {
                    get
                    {
                        int i = 0;
                        foreach (T value in _values)
                        {
                            yield return new PayloadContainers.ForUnnamedValues.InstanceBacked<T>.Entry(i++, value);
                        }
                    }
                }

                public IEnumerator<IUnnamedValuesContainerEntry> GetEnumerator()
                {
                    return new PayloadContainers.ForUnnamedValues.InstanceBacked<T>.Enumerator(this);
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
                            return new PayloadContainers.ForUnnamedValues.InstanceBacked<T>.Entry(index, _values[index]);
                        }

                        throw CreateNoSuchIndexException(index, Count);
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

                #region private struct Entry
                private struct Entry : IUnnamedValuesContainerEntry
                {
                    private readonly int _index;
                    private readonly T _value;

                    internal Entry(int index, T value)
                    {
                        if (index < 0)
                        {
                            throw new ArgumentOutOfRangeException(nameof(index));
                        }

                        _index = index;
                        _value = value;
                    }

                    public int Index { get { return _index; } }

                    public object ValueObject
                    {
                        get { return (object) _value; }
                    }

                    public TVal GetValue<TVal>()
                    {
                        return _value.Cast<T, TVal>();
                    }

                    public bool TryGetValue<TVal>(out TVal value)
                    {
                        return _value.TryCast<T, TVal>(out value);
                    }
                }
                #endregion private struct Entry

                #region private class Enumerator
                private class Enumerator : IEnumerator<IUnnamedValuesContainerEntry>
                {
                    private readonly IEnumerator<IUnnamedValuesContainerEntry> _payloadsEnumerator;

                    public Enumerator(IUnnamedValuesContainer container)
                    {
                        Validate.NotNull(container);

                        IEnumerator<IUnnamedValuesContainerEntry> payloadsEnumerator = container.Values?.GetEnumerator();
                        Validate.NotNull(payloadsEnumerator);

                        _payloadsEnumerator = payloadsEnumerator;
                    }

                    public IUnnamedValuesContainerEntry Current
                    {
                        get { return _payloadsEnumerator.Current; }
                    }

                    object IEnumerator.Current
                    {
                        get { return this.Current; }
                    }

                    public void Dispose()
                    {
                        _payloadsEnumerator.Dispose();
                    }

                    public bool MoveNext()
                    {
                        return _payloadsEnumerator.MoveNext();
                    }

                    public void Reset()
                    {
                        _payloadsEnumerator.Reset();
                    }
                }
                #endregion private class Enumerator
            }
        }
    }
}
