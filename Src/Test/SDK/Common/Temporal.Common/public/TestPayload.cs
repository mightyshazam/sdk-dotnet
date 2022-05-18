using System;
using System.Collections.Generic;
using System.Linq;
using Temporal.Common;
using Temporal.Common.Payloads;
using Xunit;

namespace Temporal.Sdk.Common.Tests
{
    public class TestPayload
    {
        [Fact]
        [Trait("Category", "Common")]
        public void Test_Payload_Unnamed_With_Null_Argument()
        {
            Assert.Throws<ArgumentNullException>(() => Payload.Unnamed(null));
        }

        [Fact]
        [Trait("Category", "Common")]
        public void Test_Payload_Unnamed_With_Variadic_Arguments()
        {
            PayloadContainers.Unnamed.InstanceBacked<object> payload = Payload.Unnamed(new object(), new object());
            AssertUnnamedCorrectness(2, payload);
        }

        [Fact]
        [Trait("Category", "Common")]
        public void Test_Payload_Unnamed_With_Array_Arguments()
        {
            PayloadContainers.Unnamed.InstanceBacked<int> payload = Payload.Unnamed(new[] { 1, 2, 3 });
            AssertUnnamedCorrectness(3, payload);
        }

        [Fact]
        public void Test_Payload_Unnamed_With_Array_Type()
        {
            PayloadContainers.Unnamed.InstanceBacked<int[]> payload = Payload.Unnamed<int[]>(new[] { 1, 2, 3 });
            AssertUnnamedCorrectness(1, payload);
            int[] value = payload.GetValue<int[]>(0);
            Assert.Equal(3, value.Length);
        }

        [Fact]
        [Trait("Category", "Common")]
        public void Test_Payload_Unnamed_With_Enumerable_Arguments()
        {
            int length = 10;
            PayloadContainers.Unnamed.InstanceBacked<string> payload = Payload.Unnamed(Enumerable.Repeat("hello", length));
            AssertUnnamedCorrectness(length, payload);
        }

        [Fact]
        [Trait("Category", "Common")]
        public void Test_Payload_Unnamed_With_List_Arguments()
        {
            int length = 10;
            IReadOnlyList<string> lst = Enumerable.Repeat("hello", length).ToList();
            PayloadContainers.Unnamed.InstanceBacked<string> payload = Payload.Unnamed(lst);
            AssertUnnamedCorrectness(length, payload);
        }

        private static void AssertUnnamedCorrectness<T>(int length,
            PayloadContainers.Unnamed.InstanceBacked<T> payload)
        {
            Assert.Equal(length, payload.Count);
            foreach (PayloadContainers.UnnamedEntry entry in payload.Values)
            {
                T value = entry.GetValue<T>();
                Assert.IsType<T>(value);
            }

            for (int i = 0; i < length; ++i)
            {
                Assert.True(payload.TryGetValue(i, out T _));
                payload.GetValue<T>(i);
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => payload.GetValue<T>(length + 1));
        }
    }
}