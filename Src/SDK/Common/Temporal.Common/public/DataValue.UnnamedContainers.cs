using System;
using System.Collections;
using System.Collections.Generic;
using Candidly.Util;

namespace Temporal.Common
{
    public static partial class DataValue
    {
        /// <summary>
        /// <c>DataValue.IUnnamedContainer</c> implementations optimized for no/few allocations when the number of value is small.
        /// </summary>
        public static class UnnamedContainers
        {
            #region struct For0
            public struct For0 : DataValue.IUnnamedContainer, IDataValue
            {
                public int Count
                {
                    get { return 0; }
                }

                public TVal GetValue<TVal>(int index)
                {
                    throw CreateNoSuchIndexException(index, Count);
                }

                public bool TryGetValue<TVal>(int index, out TVal value)
                {
                    value = default(TVal);
                    throw CreateNoSuchIndexException(index, Count);
                }

                public Type GetValueType(int index)
                {
                    throw CreateNoSuchIndexException(index, Count);
                }

                public IEnumerable<DataValue.IUnnamedContainerEntry> Values
                {
                    get { yield break; }
                }

                public IEnumerator<DataValue.IUnnamedContainerEntry> GetEnumerator()
                {
                    return new DataValue.UnnamedContainers.Enumerator(this);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return this.GetEnumerator();
                }

                public DataValue.IUnnamedContainerEntry this[int index]
                {
                    get { throw CreateNoSuchIndexException(index, Count); }
                }
            }
            #endregion struct For0

            #region struct For1<T1>
            public struct For1<T1> : DataValue.IUnnamedContainer, IDataValue
            {
                private readonly T1 _value1;

                public For1(T1 value1)
                {
                    _value1 = value1;
                }

                public int Count
                {
                    get { return 1; }
                }

                public TVal GetValue<TVal>(int index)
                {
                    return (index == 0) ? _value1.Cast<T1, TVal>()
                                : throw CreateNoSuchIndexException(index, Count);
                }

                public bool TryGetValue<TVal>(int index, out TVal value)
                {
                    return (index == 0) ? _value1.TryCast<T1, TVal>(out value)
                                : throw CreateNoSuchIndexException(index, Count);
                }

                public Type GetValueType(int index)
                {
                    return (index == 0) ? DataValue.GetTypeOfValue(_value1)
                                : throw CreateNoSuchIndexException(index, Count);
                }

                public IEnumerable<DataValue.IUnnamedContainerEntry> Values
                {
                    get
                    {
                        yield return new DataValue.UnnamedContainerEntry<T1>(0, _value1);
                    }
                }

                public IEnumerator<DataValue.IUnnamedContainerEntry> GetEnumerator()
                {
                    return new DataValue.UnnamedContainers.Enumerator(this);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return this.GetEnumerator();
                }

                public DataValue.IUnnamedContainerEntry this[int index]
                {
                    get
                    {
                        return (index == 0) ? new DataValue.UnnamedContainerEntry<T1>(0, _value1)
                                : throw CreateNoSuchIndexException(index, Count);
                    }
                }
            }
            #endregion struct For1<T1>

            #region struct For2<T1, T2>
            public struct For2<T1, T2> : DataValue.IUnnamedContainer, IDataValue
            {
                private readonly T1 _value1;
                private readonly T2 _value2;

                public For2(T1 value1, T2 value2)
                {
                    _value1 = value1;
                    _value2 = value2;
                }

                public int Count
                {
                    get { return 2; }
                }

                public TVal GetValue<TVal>(int index)
                {
                    return (index == 0) ? _value1.Cast<T1, TVal>()
                                : (index == 1) ? _value2.Cast<T2, TVal>()
                                : throw CreateNoSuchIndexException(index, Count);
                }

                public bool TryGetValue<TVal>(int index, out TVal value)
                {
                    return (index == 0) ? _value1.TryCast<T1, TVal>(out value)
                                : (index == 1) ? _value2.TryCast<T2, TVal>(out value)
                                : throw CreateNoSuchIndexException(index, Count);
                }

                public Type GetValueType(int index)
                {
                    return (index == 0) ? DataValue.GetTypeOfValue(_value1)
                                : (index == 1) ? DataValue.GetTypeOfValue(_value2)
                                : throw CreateNoSuchIndexException(index, Count);
                }

                public IEnumerable<DataValue.IUnnamedContainerEntry> Values
                {
                    get
                    {
                        yield return new DataValue.UnnamedContainerEntry<T1>(0, _value1);
                        yield return new DataValue.UnnamedContainerEntry<T2>(1, _value2);
                    }
                }

                public IEnumerator<DataValue.IUnnamedContainerEntry> GetEnumerator()
                {
                    return new DataValue.UnnamedContainers.Enumerator(this);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return this.GetEnumerator();
                }

                public DataValue.IUnnamedContainerEntry this[int index]
                {
                    get
                    {
                        return (index == 0) ? new DataValue.UnnamedContainerEntry<T1>(0, _value1)
                                : (index == 1) ? new DataValue.UnnamedContainerEntry<T2>(0, _value2)
                                : throw CreateNoSuchIndexException(index, Count);
                    }
                }
            }
            #endregion struct For2<T1, T2>

            #region struct ForN<T>        
            public struct ForN<T> : DataValue.IUnnamedContainer, IDataValue
            {
                private readonly IReadOnlyList<T> _values;

                public ForN(IReadOnlyList<T> values)
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
                        return DataValue.GetTypeOfValue(_values[index]);
                    }

                    throw CreateNoSuchIndexException(index, Count);
                }

                public IEnumerable<DataValue.IUnnamedContainerEntry> Values
                {
                    get
                    {
                        int i = 0;
                        foreach (T value in _values)
                        {
                            yield return new DataValue.UnnamedContainerEntry<T>(i++, value);
                        }
                    }
                }

                public IEnumerator<DataValue.IUnnamedContainerEntry> GetEnumerator()
                {
                    return new DataValue.UnnamedContainers.Enumerator(this);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return this.GetEnumerator();
                }

                public DataValue.IUnnamedContainerEntry this[int index]
                {
                    get
                    {
                        if (index >= 0 && index < Count)
                        {
                            return new DataValue.UnnamedContainerEntry<T>(index, _values[index]);
                        }

                        throw CreateNoSuchIndexException(index, Count);
                    }
                }
            }
            #endregion struct ForN<T>

            #region Private Utils

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
                                                          $"The {nameof(DataValue.IUnnamedContainer)} includes {containerItemCount} items,"
                                                        + $" but the {nameof(index)}=`{index}` was specified.");
                }

                return new ArgumentException(message: $"Invalid value of {nameof(index)}: {index}.", paramName: nameof(index));
            }

            private class Enumerator : IEnumerator<DataValue.IUnnamedContainerEntry>
            {
                private readonly IEnumerator<DataValue.IUnnamedContainerEntry> _dataValuesEnumerator;

                public Enumerator(DataValue.IUnnamedContainer container)
                {
                    Validate.NotNull(container);

                    IEnumerator<DataValue.IUnnamedContainerEntry> dataValuesEnumerator = container.Values?.GetEnumerator();
                    Validate.NotNull(dataValuesEnumerator);

                    _dataValuesEnumerator = dataValuesEnumerator;
                }

                public DataValue.IUnnamedContainerEntry Current
                {
                    get { return _dataValuesEnumerator.Current; }
                }

                object IEnumerator.Current
                {
                    get { return this.Current; }
                }

                public void Dispose()
                {
                    _dataValuesEnumerator.Dispose();
                }

                public bool MoveNext()
                {
                    return _dataValuesEnumerator.MoveNext();
                }

                public void Reset()
                {
                    _dataValuesEnumerator.Reset();
                }
            }

            #endregion Private Utils
        }
    }
}
