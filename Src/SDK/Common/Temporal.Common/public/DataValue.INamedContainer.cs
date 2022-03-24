using System;
using System.Collections.Generic;

namespace Temporal.Common
{
    public static partial class DataValue
    {
        public interface INamedContainer : IDataValue,
                                           IReadOnlyDictionary<string, DataValue.INamedContainerEntry>
        {
            new int Count { get; }

            TVal GetValue<TVal>(string name);
            bool TryGetValue<TVal>(string name, out TVal value);

            Type GetValueType(string name);
            bool TryGetValueType(string name, out Type valueType);

            new bool ContainsKey(string name);

            new IEnumerable<string> Keys { get; }
            new IEnumerable<DataValue.INamedContainerEntry> Values { get; }
        }
    }
}
