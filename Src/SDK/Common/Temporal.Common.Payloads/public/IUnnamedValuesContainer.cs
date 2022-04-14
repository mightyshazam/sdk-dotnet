using System;
using System.Collections.Generic;

namespace Temporal.Common.Payloads
{
    public interface IUnnamedValuesContainer : IPayload,
                                               IReadOnlyList<UnnamedValuesContainerEntry>
    {
        new int Count { get; }

        TVal GetValue<TVal>(int index);
        bool TryGetValue<TVal>(int index, out TVal value);

        IEnumerable<UnnamedValuesContainerEntry> Values { get; }
    }
}
