using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Temporal.Common.DataModel
{
    public class CommonDataModelApi
    {
    }

    public interface IDataValue
    {     
        public sealed class Void : IDataValue
        {            
            public static readonly Void Instance = new Void();
            public static readonly Task<Void> CompletedTask = Task.FromResult(Instance);            
        }
    }

    public static class DataValue
    {
        public static readonly IDataValue.Void Void = IDataValue.Void.Instance;
        public static readonly Task<IDataValue.Void> VoidTask = IDataValue.Void.CompletedTask;

        public static IDataValue Adapt<T>(string name, T value) { return new DataValue.Adapters.For1<T>(name, value); }
        public static IDataValue Adapt<T1, T2>(string name1, T1 value1, string name2, T2 value2) { return new DataValue.Adapters.For2<T1, T2>(name1, value1, name2, value2); }
        public static IDataValue Adapt<T>(params object[] namedValues) { return new DataValue.Adapters.ForN<T, object>(namedValues); }
        public static IDataValue Adapt<T>(params KeyValuePair<string, T>[] namedValues) { return new DataValue.Adapters.ForN<T, KeyValuePair<string, T>>(namedValues); }

        public static IDataValue Wrap<T>(T value) { return new DataValue.Wrappers.For1<T>(value); }
        public static IDataValue Wrap<T1, T2>(T1 value1, T2 value2) { return new DataValue.Wrappers.For2<T1, T2>(value1, value2); }
        public static IDataValue Wrap<T>(params T[] values) { return new DataValue.Wrappers.ForN<T>(values); }

        public static bool TryUnpack<T>(IDataValue data, out T value)
        {
            value = default(T);

            if (data == null)
            {
                return false;
            }

            {
                if (data is T packedData)
                {
                    value = packedData;
                    return true;
                }
            }
            {
                if (data is DataValue.IPackedDataValue packedData && packedData.Count >= 1 && packedData.TryGetValue<T>(0, out value))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryUnpack<T1, T2>(IDataValue data, out T1 value1, out T2 value2)
        {
            value1 = default(T1);
            value2 = default(T2);

            bool success1 = false;
            bool success2 = false;

            if (data == null)
            {
                return false;
            }

            {
                if (data is DataValue.IPackedDataValue packedData && packedData.Count >= 1 && packedData.TryGetValue<T1>(0, out value1))
                {
                    success1 = true;
                }
            }
            {
                if (data is DataValue.IPackedDataValue packedData && packedData.Count >= 2 && packedData.TryGetValue<T2>(1, out value2))
                {
                    success2 = true;
                }
            }

            return success1 && success2;
        }

        public static bool TryUnpack<T>(IDataValue data, IList<T> unpackedValuesBuff)
        {
            if (data == null && unpackedValuesBuff == null)
            {
                return false;
            }

            if (data == null || data is not DataValue.IPackedDataValue)
            {
                for (int i = 0; i < unpackedValuesBuff.Count; i++)
                {
                    unpackedValuesBuff[i] = default(T);
                }

                return false;
            }

            DataValue.IPackedDataValue packedData = (DataValue.IPackedDataValue) data;

            bool success = true;
            for (int i = 0; i < unpackedValuesBuff.Count; i++)
            {
                if (packedData.Count >= i + 1 && packedData.TryGetValue<T>(i, out T val))
                {
                    unpackedValuesBuff[i] = val;
                }
                else
                {
                    unpackedValuesBuff[i] = default(T);
                    success = false;
                }
            }

            return success;                
        }

        public interface IPackedDataValue : IDataValue
        {
            int Count { get; }
            TVal GetValue<TVal>(int index);
            bool TryGetValue<TVal>(int index, out TVal value);
        }

        public interface IAdapter : IPackedDataValue
        {
            string GetName(int index);
        }

        public interface IWrapper : IPackedDataValue
        {
        }

        public static class Adapters
        {
            public struct For1<T1> : IAdapter
            {
                private readonly string _name1;
                private readonly T1 _value1;

                public For1(string name1, T1 value1)
                {
                    _name1 = name1;
                    _value1 = value1;
                }

                public string Name1 { get { return _name1; } }
                public T1 Value1 { get { return _value1; } }

                public int Count { get { return 1; } }

                public string GetName(int index)
                {
                    return (index == 0) ? _name1 : throw new ArgumentOutOfRangeException(nameof(index));
                }

                public TVal GetValue<TVal>(int index)
                {
                    return (index == 0) ? Cast<T1, TVal>(_value1) : throw new ArgumentOutOfRangeException(nameof(index));
                }

                public bool TryGetValue<TVal>(int index, out TVal value)
                {
                    return (index == 0) ? TryCast<T1, TVal>(_value1, out value) : throw new ArgumentOutOfRangeException(nameof(index));
                }
            }

            public struct For2<T1, T2> : IAdapter
            {
                private readonly string _name1, _name2;
                private readonly T1 _value1;
                private readonly T2 _value2;

                public For2(string name1, T1 value1, string name2, T2 value2)
                {
                    _name1 = name1;
                    _value1 = value1;
                    _name2 = name2;
                    _value2 = value2;
                }

                public string Name1 { get { return _name1; } }
                public T1 Value1 { get { return _value1; } }
                public string Name2 { get { return _name2; } }
                public T2 Value2 { get { return _value2; } }

                public int Count { get { return 2; } }

                public string GetName(int index)
                {
                    return (index == 0) ? _name1
                            : (index == 1) ? _name2
                            : throw new ArgumentOutOfRangeException(nameof(index));
                }

                public TVal GetValue<TVal>(int index)
                {
                    return (index == 0) ? Cast<T1, TVal>(_value1)
                            : (index == 1) ? Cast<T2, TVal>(_value2)
                            : throw new ArgumentOutOfRangeException(nameof(index));
                }

                public bool TryGetValue<TVal>(int index, out TVal value)
                {
                    return (index == 0) ? TryCast<T1, TVal>(_value1, out value)
                            : (index == 1) ? TryCast<T2, TVal>(_value2, out value)
                            : throw new ArgumentOutOfRangeException(nameof(index));
                }
            }

            public struct ForN<T, TNamedValsItem> : IAdapter
            {
                private readonly int _count;
                private readonly IReadOnlyList<TNamedValsItem> _namedValues;
                private readonly bool _useKeyValPairs;

                public ForN(IReadOnlyList<object> namedValues)
                {
                    if (typeof(TNamedValsItem) == typeof(object))
                    {
                        throw new InvalidOperationException($"This ctor may only be used when the type parameter {nameof(TNamedValsItem)}"
                                                          + $" is \"{nameof(Object)}\".");
                    }

                    _useKeyValPairs = false;

                    if (namedValues.Count % 2 != 0)
                    {
                        throw new ArgumentException($"{nameof(namedValues)} must have an even number of elements, but it has {namedValues.Count}.");
                    }

                    _count = namedValues.Count / 2;
                    _namedValues = (IReadOnlyList<TNamedValsItem>) namedValues;

                    for (int v = 0; v < _count; v++)
                    {
                        object name = namedValues[v * 2];
                        object value = namedValues[v * 2 + 1];

                        if (name == null)
                        {
                            throw new ArgumentException($"{nameof(namedValues)}[{v * 2}] is expected to be a String denoting"
                                                      + $" the name of the value #{v}, but it is null.");
                        }

                        if (name is not string)
                        {
                            throw new ArgumentException($"{nameof(namedValues)}[{v * 2}] is expected to be a String denoting"
                                                      + $" the name of the value #{v}, but it is an instance"
                                                      + $" of type \"{name.GetType().FullName}\", which is not a String.");
                        }

                        if (value is not T)
                        {
                            throw new ArgumentException($"{nameof(namedValues)}[{v * 2 + 1}] is expected to be an instance of"
                                                      + $" type \"{typeof(T).FullName}\" representing the value #{v},"
                                                      + $" but instead it is "
                                                      + (value == null ? "null" : $"an instance of type \"{value.GetType().FullName}\"")
                                                      + ".");
                        }
                    }
                }

                public ForN(IReadOnlyList<KeyValuePair<string, T>> namedValues)
                {
                    _useKeyValPairs = (typeof(TNamedValsItem) == typeof(KeyValuePair<string, T>));

                    if (!_useKeyValPairs)
                    {
                        throw new InvalidOperationException($"This ctor may only be used when the type parameter {nameof(TNamedValsItem)}"
                                                          + $" is \"{typeof(KeyValuePair<string, T>).FullName}\".");
                    }

                    _count = namedValues.Count;
                    _namedValues = (IReadOnlyList<TNamedValsItem>) namedValues;

                    for (int v = 0; v < _count; v++)
                    {
                        string name = namedValues[v].Key;
                        T value = namedValues[v].Value;

                        if (name == null)
                        {
                            throw new ArgumentException($"{nameof(namedValues)}[{v * 2}] is expected to be a String denoting"
                                                      + $" the name of the value #{v}, but it is null.");
                        }
                    }
                }

                public KeyValuePair<string, T> this[int index]
                {
                    get
                    {
                        if (!_useKeyValPairs)
                        {
                            return new KeyValuePair<string, T>(Cast<object, string>(_namedValues[index * 2]),
                                                               Cast<object, T>(_namedValues[index * 2 + 1]));
                        }
                        else
                        {
                            return Cast<TNamedValsItem, KeyValuePair<string, T>>(_namedValues[index]);
                        }
                    }
                }

                public int Count { get { return _count; } }

                public string GetName(int index)
                {
                    if (!_useKeyValPairs)
                    {
                        return Cast<object, string>(_namedValues[index * 2]);
                    }
                    else
                    {                        
                        return Cast<TNamedValsItem, KeyValuePair<string, T>>(_namedValues[index]).Key;
                    }
                }

                public TVal GetValue<TVal>(int index)
                {                    
                    if (!_useKeyValPairs)
                    {
                        return Cast<object, TVal>(_namedValues[index * 2 + 1]);
                    }
                    else
                    {
                        return Cast<T, TVal>(Cast<TNamedValsItem, KeyValuePair<string, T>>(_namedValues[index]).Value);
                    }
                }

                public bool TryGetValue<TVal>(int index, out TVal value)
                {
                    if (!_useKeyValPairs)
                    {
                        return TryCast<object, TVal>(_namedValues[index * 2 + 1], out value);
                    }
                    else
                    {
                        return TryCast<T, TVal>(Cast<TNamedValsItem, KeyValuePair<string, T>>(_namedValues[index]).Value, out value);
                    }
                }
            }
        }

        public static class Wrappers
        { 
            public struct For1<T1> : IWrapper
            {
                private readonly T1 _value1;

                public For1(T1 value1)
                {
                    _value1 = value1;
                }

                public T1 Value1 { get { return _value1; } }

                public int Count { get { return 1; } }

                public TVal GetValue<TVal>(int index)
                {
                    return (index == 0) ? Cast<T1, TVal>(_value1)
                            : throw new ArgumentOutOfRangeException(nameof(index));
                }

                public bool TryGetValue<TVal>(int index, out TVal value)
                {
                    return (index == 0) ? TryCast<T1, TVal>(_value1, out value)
                            : throw new ArgumentOutOfRangeException(nameof(index));
                }
            }

            public struct For2<T1, T2> : IWrapper
            {
                private readonly T1 _value1;
                private readonly T2 _value2;

                public For2(T1 value1, T2 value2)
                {
                    _value1 = value1;
                    _value2 = value2;
                }

                public T1 Value1 { get { return _value1; } }
                public T2 Value2 { get { return _value2; } }

                public int Count { get { return 2; } }

                public TVal GetValue<TVal>(int index)
                {
                    return (index == 0) ? Cast<T1, TVal>(_value1)
                            : (index == 1) ? Cast<T2, TVal>(_value2)
                            : throw new ArgumentOutOfRangeException(nameof(index));
                }

                public bool TryGetValue<TVal>(int index, out TVal value)
                {
                    return (index == 0) ? TryCast<T1, TVal>(_value1, out value)
                            : (index == 1) ? TryCast<T2, TVal>(_value2, out value)
                            : throw new ArgumentOutOfRangeException(nameof(index));
                }
            }

            public struct ForN<T> : IWrapper
            {
                private readonly T[] _values;

                public ForN(T[] values)
                {
                    _values = values;
                }

                public T this[int index] { get { return _values[index]; } }

                public int Count { get { return ((_values == null) ? 0 : _values.Length); } }

                public TVal GetValue<TVal>(int index)
                {
                    return Cast<T, TVal>(_values[index]);
                }

                public bool TryGetValue<TVal>(int index, out TVal value)
                {
                    return TryCast<object, TVal>(_values[index * 2 + 1], out value);
                }
            }
        }  // interface Wrappers
    }  // class DataValue


    public class PayloadsCollection : IReadOnlyList<Payload>
    {
        public static readonly PayloadsCollection Empty = new PayloadsCollection();

        public int Count { get; }
        public Payload this[int index] { get { return null; } }
        public IEnumerator<Payload> GetEnumerator() { return null; }
        IEnumerator IEnumerable.GetEnumerator() { return null; }
    }

    public class MutablePayloadsCollection : PayloadsCollection, ICollection<Payload>
    {
        public bool IsReadOnly { get { return false; } }
        public void Add(Payload item) { }
        public void Clear() { }
        public bool Contains(Payload item) { return false; }
        public void CopyTo(Payload[] array, int arrayIndex) { }
        public bool Remove(Payload item) { return false; }        
    }

    public class Payload
    {        
        public IReadOnlyDictionary<string, Stream> Metadata { get; }
        public Stream Data { get; }
    }

    public class MutablePayload : Payload
    {
        public IDictionary<string, Stream> MutableMetadata { get; }
        public Stream MutableData { get; }

        public void CopyMetadataFrom(Payload payload) { }
        public Stream RemoveMetadataEntry(string key) { return null; }
        public void SetMetadataEntry(string key, string data) { }        
        public void SetMetadataEntry(string key, byte[] data) { }
        public void SetMetadataEntry(string key, int data) { }
        public void SetMetadataEntry(string key) { }
    }

    public enum WorkflowExecutionStatus
    {
        Unspecified = 0,
        Running = 1,
        Completed = 2,
        Failed = 3,
        Canceled = 4,
        Terminated = 5,
        ContinuedAsNew = 6,
        TimedOut = 7,
    }

    public enum TimeoutType
    {
        Unspecified = 0,
        StartToClose = 1,
        ScheduleToStart = 2,
        ScheduleToClose = 3,
        Heartbeat = 4,
    }

    public enum RetryState
    {
        Unspecified = 0,
        InProgress = 1,
        NonRetriableFailure = 2,
        Timeout = 3,
        MaximumAttemptsReached = 4,
        RetryPolicyNotSet = 5,
        InternalServerError = 6,
        CancelRequested = 7,
    }
}
