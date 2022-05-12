using System;

namespace Temporal.Common.Payloads
{
    public static partial class PayloadContainers
    {
        internal static class Util
        {
            public static ArgumentException CreateNoSuchIndexException<TCnt>(int index, int containerItemCount, TCnt containerInstance)
                                                                           where TCnt : PayloadContainers.IUnnamed
            {
                if (index < 0)
                {
                    return new ArgumentOutOfRangeException(nameof(index), $"The value of {nameof(index)} may not be negative,"
                                                                        + $" but `{index}` was specified.");
                }

                if (index >= containerItemCount)
                {
                    return new ArgumentOutOfRangeException(nameof(index),
                                                           $"This {containerInstance.GetType().Name} includes"
                                                         + $" {containerItemCount} items, but the {nameof(index)}=`{index}` was specified.");
                }

                return new ArgumentException(message: $"Invalid value of {nameof(index)}: {index}.", paramName: nameof(index));
            }
        }
    }
}
