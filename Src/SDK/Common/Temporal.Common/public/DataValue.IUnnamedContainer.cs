using System;
using System.Collections.Generic;

namespace Temporal.Common
{
    public static partial class DataValue
    {
        public interface IUnnamedContainer : IDataValue,
                                             IReadOnlyList<DataValue.IUnnamedContainerEntry>
        {
            new int Count { get; }

            TVal GetValue<TVal>(int index);
            bool TryGetValue<TVal>(int index, out TVal value);

            Type GetValueType(int index);

            IEnumerable<DataValue.IUnnamedContainerEntry> Values { get; }
        }
    }
}
