using System.Collections.Generic;

namespace Temporal.Common.Payloads
{
    public static partial class PayloadContainers
    {
        public interface IUnnamed : IPayload,
                                    IReadOnlyList<PayloadContainers.UnnamedEntry>
        {
            new int Count { get; }

            TVal GetValue<TVal>(int index);
            bool TryGetValue<TVal>(int index, out TVal value);

            IEnumerable<PayloadContainers.UnnamedEntry> Values { get; }
        }
    }
}
