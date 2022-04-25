using System;
using System.Collections.Generic;
using Temporal.Util;
using Google.Protobuf.Collections;
using Temporal.Api.Common.V1;

namespace Temporal.Serialization
{
    internal static class SerializationUtil
    {
        public static List<T> EnsureIsList<T>(IEnumerable<T> items)
        {
            if (items == null)
            {
                return new List<T>(capacity: 0);
            }

            if (items is List<T> itemsList)
            {
                return itemsList;
            }

            List<T> newList = (items is IReadOnlyCollection<T> itemsCollection)
                                    ? new List<T>(capacity: itemsCollection.Count)
                                    : new List<T>();

            foreach (T item in items)
            {
                newList.Add(item);
            }

            return newList;
        }

        public static void Add(Payloads serializedDataAccumulator, Payload serializedItemData)
        {
            if (serializedItemData == null)
            {
                return;
            }

            Validate.NotNull(serializedDataAccumulator);
            Validate.NotNull(serializedDataAccumulator.Payloads_);

            serializedDataAccumulator.Payloads_.Add(serializedItemData);
        }

        public static int GetPayloadCount(Payloads payloads)
        {
            return GetPayloadCount(payloads, out RepeatedField<Payload> _);
        }

        public static int GetPayloadCount(Payloads payloads, out IReadOnlyList<Payload> payloadCollection)
        {
            int c = GetPayloadCount(payloads, out RepeatedField<Payload> payloadField);
            payloadCollection = payloadField;
            return c;
        }

        public static int GetPayloadCount(Payloads payloads, out RepeatedField<Payload> payloadCollection)
        {
            if (payloads == null || payloads.Payloads_ == null)
            {
                payloadCollection = null;
                return 0;
            }

            payloadCollection = payloads.Payloads_;
            return payloadCollection.Count;
        }
    }
}
