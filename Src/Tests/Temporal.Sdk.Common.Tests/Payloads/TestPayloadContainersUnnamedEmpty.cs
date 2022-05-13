using System;
using Temporal.Common.Payloads;
using Xunit;

namespace Temporal.Sdk.Common.Tests
{
    public class TestPayloadContainersUnnamedEmpty : AbstractUnnamedTest
    {
        public TestPayloadContainersUnnamedEmpty()
            : base(new PayloadContainers.Unnamed.Empty(), 0)
        {
        }

        [Fact]
        public void Test_PayloadContainers_Unnamed_Empty()
        {
            var empty = new PayloadContainers.Unnamed.Empty();
            Assert.Empty(empty);
            Assert.Throws<ArgumentOutOfRangeException>(() => empty.GetValue<object>(0));
            Assert.False(empty.TryGetValue<object>(0, out _));
        }
    }
}