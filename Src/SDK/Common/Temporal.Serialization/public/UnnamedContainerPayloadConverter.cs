using System;
using Candidly.Util;
using Temporal.Api.Common.V1;
using Temporal.Common.Payloads;

namespace Temporal.Serialization
{
    public sealed class UnnamedContainerPayloadConverter : DelegatingPayloadConverterBase
    {
        public override bool TrySerialize<T>(T item, Payloads serializedDataAccumulator)
        {
            // If item is an PayloadContainers.IUnnamed => delegate each contained unnamed value separately to the downstream converters.
            // Otherwise => This converter cannot handle it.

            if (item != null && item is PayloadContainers.IUnnamed itemsContainer)
            {
                Validate.NotNull(serializedDataAccumulator);

                for (int i = 0; i < itemsContainer.Count; i++)
                {
                    try
                    {
                        PayloadConverter.Serialize(DelegateConvertersContainer, itemsContainer.GetValue<object>(i), serializedDataAccumulator);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Error serializing value at index {i} of the specified container"
                                                          + $" of type \"{itemsContainer.GetType().FullName}\".",
                                                            ex);
                    }
                }

                return true;
            }

            return false;
        }

        public override bool TryDeserialize<T>(Payloads serializedData, out T deserializedItem)
        {
            Validate.NotNull(serializedData);

            // `PayloadContainers.Unnamed.SerializedDataBacked` is a container that supports strictly typed
            // lazy deserialization of data when the value is actually requested.
            // It supports multiple `Payload`-entries within the `Payloads`-collection.
            // That container is be used by SDK to offer APIs that access data when needed.

            // We can handle the conversion
            // if the user asked for any type `T` that can be assigned to `PayloadContainers.Unnamed.SerializedDataBacked`
            // OR if the user asked for any `PayloadContainers.IUnnamed`.

            if (typeof(PayloadContainers.Unnamed.SerializedDataBacked).IsAssignableFrom(typeof(T))
                    || typeof(PayloadContainers.IUnnamed) == typeof(T))
            {
                PayloadContainers.Unnamed.SerializedDataBacked container = new(serializedData, DelegateConvertersContainer);
                deserializedItem = container.Cast<PayloadContainers.Unnamed.SerializedDataBacked, T>();
                return true;
            }

            deserializedItem = default(T);
            return false;
        }
    }
}
