using System;
using System.Collections;
using System.Collections.Generic;
using Candidly.Util;

namespace Temporal.Common
{
    public static partial class DataValue
    {
        /// <summary>
        /// <c>DataValue.INamedContainer</c> implementations optimized for no/few allocations when the number of value is small.
        /// </summary>
        public static class NamedContainers
        {
            #region struct For0
            public struct For0 : DataValue.INamedContainer, IDataValue
            {
                public int Count
                {
                    get { return 0; }
                }

                public TVal GetValue<TVal>(string name)
                {
                    Validate.NotNullOrWhitespace(name);
                    throw CreateNoSuchNamedValueException(name);
                }

                public bool TryGetValue<TVal>(string name, out TVal value)
                {
                    Validate.NotNullOrWhitespace(name);
                    value = default(TVal);
                    return false;
                }

                public Type GetValueType(string name)
                {
                    Validate.NotNullOrWhitespace(name);
                    throw CreateNoSuchNamedValueException(name);
                }

                public bool TryGetValueType(string name, out Type valueType)
                {
                    Validate.NotNullOrWhitespace(name);
                    valueType = null;
                    return false;
                }

                public bool ContainsKey(string name)
                {
                    return false;
                }

                public IEnumerable<string> Keys
                {
                    get { yield break; }
                }

                public IEnumerable<DataValue.INamedContainerEntry> Values
                {
                    get { yield break; }
                }

                public IEnumerator<KeyValuePair<string, DataValue.INamedContainerEntry>> GetEnumerator()
                {
                    return new DataValue.NamedContainers.Enumerator(this);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return this.GetEnumerator();
                }

                bool IReadOnlyDictionary<string, DataValue.INamedContainerEntry>.TryGetValue(string name, out DataValue.INamedContainerEntry entry)
                {
                    Validate.NotNullOrWhitespace(name);
                    entry = default(DataValue.INamedContainerEntry);
                    return false;
                }

                public DataValue.INamedContainerEntry this[string name]
                {
                    get
                    {
                        Validate.NotNullOrWhitespace(name);
                        throw CreateNoSuchNamedValueException(name);
                    }
                }
            }
            #endregion struct For0

            #region struct For1<T1>
            public struct For1<T1> : DataValue.INamedContainer, IDataValue
            {
                private readonly string _name1;
                private readonly T1 _value1;

                public For1(string name1, T1 value1)
                {
                    Validate.NotNullOrWhitespace(name1);

                    _name1 = name1;
                    _value1 = value1;
                }

                public int Count
                {
                    get { return 1; }
                }

                public TVal GetValue<TVal>(string name)
                {
                    Validate.NotNullOrWhitespace(name);

                    if (name.Equals(_name1, StringComparison.Ordinal))
                    {
                        return _value1.Cast<T1, TVal>();
                    }

                    throw CreateNoSuchNamedValueException(name);
                }

                public bool TryGetValue<TVal>(string name, out TVal value)
                {
                    Validate.NotNullOrWhitespace(name);

                    if (name.Equals(_name1, StringComparison.Ordinal))
                    {
                        return _value1.TryCast<T1, TVal>(out value);
                    }

                    value = default(TVal);
                    return false;
                }

                public Type GetValueType(string name)
                {
                    Validate.NotNullOrWhitespace(name);

                    if (name.Equals(_name1, StringComparison.Ordinal))
                    {
                        return DataValue.GetTypeOfValue(_value1);
                    }

                    throw CreateNoSuchNamedValueException(name);
                }

                public bool TryGetValueType(string name, out Type valueType)
                {
                    Validate.NotNullOrWhitespace(name);

                    if (name.Equals(_name1, StringComparison.Ordinal))
                    {
                        valueType = DataValue.GetTypeOfValue(_value1);
                        return true;
                    }

                    valueType = null;
                    return false;
                }

                public bool ContainsKey(string name)
                {
                    return !String.IsNullOrWhiteSpace(name)
                                && name.Equals(_name1, StringComparison.Ordinal);
                }

                public IEnumerable<string> Keys
                {
                    get { yield return _name1; }
                }

                public IEnumerable<DataValue.INamedContainerEntry> Values
                {
                    get { yield return new DataValue.NamedContainerEntry<T1>(_name1, _value1); }
                }

                public IEnumerator<KeyValuePair<string, DataValue.INamedContainerEntry>> GetEnumerator()
                {
                    return new DataValue.NamedContainers.Enumerator(this);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return this.GetEnumerator();
                }

                bool IReadOnlyDictionary<string, DataValue.INamedContainerEntry>.TryGetValue(string name, out DataValue.INamedContainerEntry entry)
                {
                    Validate.NotNullOrWhitespace(name);

                    if (name.Equals(_name1, StringComparison.Ordinal))
                    {
                        entry = new DataValue.NamedContainerEntry<T1>(_name1, _value1);
                        return true;
                    }

                    entry = default(DataValue.INamedContainerEntry);
                    return false;
                }

                public DataValue.INamedContainerEntry this[string name]
                {
                    get
                    {
                        Validate.NotNullOrWhitespace(name);

                        if (name.Equals(_name1, StringComparison.Ordinal))
                        {
                            return new DataValue.NamedContainerEntry<T1>(_name1, _value1);
                        }

                        throw CreateNoSuchNamedValueException(name);
                    }
                }
            }
            #endregion struct For1<T1>

            #region struct For2<T1, T2>
            public struct For2<T1, T2> : DataValue.INamedContainer, IDataValue
            {
                private readonly string _name1;
                private readonly string _name2;
                private readonly T1 _value1;
                private readonly T2 _value2;

                public For2(string name1, T1 value1, string name2, T2 value2)
                {
                    Validate.NotNullOrWhitespace(name1);
                    Validate.NotNullOrWhitespace(name2);

                    if (name1.Equals(name2, StringComparison.Ordinal))
                    {
                        throw new ArgumentException($"An {nameof(DataValue.INamedContainer)} must not have duplicate names, however,"
                                                  + $" {nameof(name1)} equals {nameof(name1)} (\"{name1}\")");
                    }

                    _name1 = name1;
                    _value1 = value1;

                    _name2 = name2;
                    _value2 = value2;
                }

                public int Count
                {
                    get { return 2; }
                }

                public TVal GetValue<TVal>(string name)
                {
                    Validate.NotNullOrWhitespace(name);

                    if (name.Equals(_name1, StringComparison.Ordinal))
                    {
                        return _value1.Cast<T1, TVal>();
                    }
                    else if (name.Equals(_name2, StringComparison.Ordinal))
                    {
                        return _value2.Cast<T2, TVal>();
                    }

                    throw CreateNoSuchNamedValueException(name);
                }

                public bool TryGetValue<TVal>(string name, out TVal value)
                {
                    Validate.NotNullOrWhitespace(name);

                    if (name.Equals(_name1, StringComparison.Ordinal))
                    {
                        return _value1.TryCast<T1, TVal>(out value);
                    }
                    else if (name.Equals(_name2, StringComparison.Ordinal))
                    {
                        return _value2.TryCast<T2, TVal>(out value);
                    }

                    value = default(TVal);
                    return false;
                }

                public Type GetValueType(string name)
                {
                    Validate.NotNullOrWhitespace(name);

                    if (name.Equals(_name1, StringComparison.Ordinal))
                    {
                        return DataValue.GetTypeOfValue(_value1);
                    }
                    else if (name.Equals(_name2, StringComparison.Ordinal))
                    {
                        return DataValue.GetTypeOfValue(_value2);
                    }

                    throw CreateNoSuchNamedValueException(name);
                }

                public bool TryGetValueType(string name, out Type valueType)
                {
                    Validate.NotNullOrWhitespace(name);

                    if (name.Equals(_name1, StringComparison.Ordinal))
                    {
                        valueType = DataValue.GetTypeOfValue(_value1);
                        return true;
                    }
                    else if (name.Equals(_name2, StringComparison.Ordinal))
                    {
                        valueType = DataValue.GetTypeOfValue(_value2);
                        return true;
                    }

                    valueType = null;
                    return false;
                }

                public bool ContainsKey(string name)
                {
                    if (String.IsNullOrWhiteSpace(name))
                    {
                        return false;
                    }

                    return name.Equals(_name1, StringComparison.Ordinal)
                                || name.Equals(_name1, StringComparison.Ordinal);
                }

                public IEnumerable<string> Keys
                {
                    get
                    {
                        yield return _name1;
                        yield return _name2;
                    }
                }

                public IEnumerable<DataValue.INamedContainerEntry> Values
                {
                    get
                    {
                        yield return new DataValue.NamedContainerEntry<T1>(_name1, _value1);
                        yield return new DataValue.NamedContainerEntry<T2>(_name2, _value2);
                    }
                }

                public IEnumerator<KeyValuePair<string, DataValue.INamedContainerEntry>> GetEnumerator()
                {
                    return new DataValue.NamedContainers.Enumerator(this);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return this.GetEnumerator();
                }

                bool IReadOnlyDictionary<string, DataValue.INamedContainerEntry>.TryGetValue(string name, out DataValue.INamedContainerEntry entry)
                {
                    Validate.NotNullOrWhitespace(name);

                    if (name.Equals(_name1, StringComparison.Ordinal))
                    {
                        entry = new DataValue.NamedContainerEntry<T1>(_name1, _value1);
                        return true;
                    }
                    else if (name.Equals(_name2, StringComparison.Ordinal))
                    {
                        entry = new DataValue.NamedContainerEntry<T2>(_name2, _value2);
                        return true;
                    }

                    entry = default(DataValue.INamedContainerEntry);
                    return false;
                }

                public DataValue.INamedContainerEntry this[string name]
                {
                    get
                    {
                        Validate.NotNullOrWhitespace(name);

                        if (name.Equals(_name1, StringComparison.Ordinal))
                        {
                            return new DataValue.NamedContainerEntry<T1>(_name1, _value1);
                        }
                        else if (name.Equals(_name2, StringComparison.Ordinal))
                        {
                            return new DataValue.NamedContainerEntry<T2>(_name2, _value2);
                        }

                        throw CreateNoSuchNamedValueException(name);
                    }
                }
            }
            #endregion struct For2<T1, T2>

            #region struct ForN<T>        
            public struct ForN<T> : DataValue.INamedContainer, IDataValue
            {
                private readonly IReadOnlyDictionary<string, T> _namedValues;

                public ForN(IReadOnlyDictionary<string, T> namedValues)
                {
                    Validate.NotNull(namedValues);

                    _namedValues = namedValues;
                }

                public int Count
                {
                    get { return _namedValues.Count; }
                }

                public TVal GetValue<TVal>(string name)
                {
                    Validate.NotNullOrWhitespace(name);

                    if (_namedValues.TryGetValue(name, out T value))
                    {
                        return value.Cast<T, TVal>();
                    }

                    throw CreateNoSuchNamedValueException(name);
                }

                public bool TryGetValue<TVal>(string name, out TVal value)
                {
                    Validate.NotNullOrWhitespace(name);

                    if (_namedValues.TryGetValue(name, out T val))
                    {
                        return val.TryCast<T, TVal>(out value);
                    }

                    value = default(TVal);
                    return false;
                }

                public Type GetValueType(string name)
                {
                    Validate.NotNullOrWhitespace(name);

                    if (_namedValues.TryGetValue(name, out T value))
                    {
                        return DataValue.GetTypeOfValue<T>(value);
                    }

                    throw CreateNoSuchNamedValueException(name);
                }

                public bool TryGetValueType(string name, out Type valueType)
                {
                    Validate.NotNullOrWhitespace(name);

                    if (_namedValues.TryGetValue(name, out T value))
                    {
                        valueType = DataValue.GetTypeOfValue<T>(value);
                        return true;
                    }

                    valueType = null;
                    return false;
                }

                public bool ContainsKey(string name)
                {
                    if (String.IsNullOrWhiteSpace(name))
                    {
                        return false;
                    }

                    return _namedValues.ContainsKey(name);
                }

                public IEnumerable<string> Keys
                {
                    get { return _namedValues.Keys; }
                }

                public IEnumerable<DataValue.INamedContainerEntry> Values
                {
                    get
                    {
                        foreach (KeyValuePair<string, T> namedValue in _namedValues)
                        {
                            yield return new DataValue.NamedContainerEntry<T>(namedValue.Key, namedValue.Value);
                        }
                    }
                }

                public IEnumerator<KeyValuePair<string, DataValue.INamedContainerEntry>> GetEnumerator()
                {
                    return new DataValue.NamedContainers.Enumerator(this);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return this.GetEnumerator();
                }

                bool IReadOnlyDictionary<string, DataValue.INamedContainerEntry>.TryGetValue(string name, out DataValue.INamedContainerEntry entry)
                {
                    Validate.NotNullOrWhitespace(name);

                    if (_namedValues.TryGetValue(name, out T value))
                    {
                        entry = new DataValue.NamedContainerEntry<T>(name, value);
                        return true;
                    }

                    entry = default(DataValue.INamedContainerEntry);
                    return false;
                }

                public DataValue.INamedContainerEntry this[string name]
                {
                    get
                    {
                        Validate.NotNullOrWhitespace(name);

                        try
                        {
                            return new DataValue.NamedContainerEntry<T>(name, _namedValues[name]);
                        }
                        catch (KeyNotFoundException knfEx)
                        {
                            throw CreateNoSuchNamedValueException(name, knfEx);
                        }
                    }
                }
            }
            #endregion struct ForN<T>

            #region Private Utils

            private static KeyNotFoundException CreateNoSuchNamedValueException(string name)
            {
                return new KeyNotFoundException($"This {nameof(DataValue.INamedContainer)} does not contain a value with"
                                              + $" the specified {nameof(name)} \"{name}\".");
            }

            private static KeyNotFoundException CreateNoSuchNamedValueException(string name, Exception innerException)
            {
                return new KeyNotFoundException($"This {nameof(DataValue.INamedContainer)} does not contain a value with"
                                              + $" the specified {nameof(name)} \"{name}\".",
                                                innerException);
            }

            private class Enumerator : IEnumerator<KeyValuePair<string, DataValue.INamedContainerEntry>>
            {
                private readonly IEnumerator<DataValue.INamedContainerEntry> _dataValuesEnumerator;

                public Enumerator(DataValue.INamedContainer container)
                {
                    Validate.NotNull(container);

                    IEnumerator<DataValue.INamedContainerEntry> dataValuesEnumerator = container.Values?.GetEnumerator();
                    Validate.NotNull(dataValuesEnumerator);

                    _dataValuesEnumerator = dataValuesEnumerator;
                }

                public KeyValuePair<string, DataValue.INamedContainerEntry> Current
                {
                    get
                    {
                        DataValue.INamedContainerEntry dv = _dataValuesEnumerator.Current;
                        return new KeyValuePair<string, DataValue.INamedContainerEntry>(dv.Name, dv);
                    }
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
