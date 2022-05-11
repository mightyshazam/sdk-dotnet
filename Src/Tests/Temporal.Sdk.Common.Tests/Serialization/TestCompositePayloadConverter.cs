using Temporal.Api.Common.V1;
using Temporal.Common;
using Temporal.Common.Payloads;
using Temporal.Serialization;
using Xunit;

namespace Temporal.Sdk.Common.Tests.Serialization
{
    public class TestCompositePayloadConverter
    {
        [Fact]
        public void Test_CompositePayloadConverter_Void_Roundtrip()
        {
            var converter = new CompositePayloadConverter();
            var p = new Payloads();
            Assert.True(converter.TrySerialize(new IPayload.Void(), p));
            Assert.Empty(p.Payloads_);
            Assert.True(converter.TryDeserialize(p, out IPayload.Void item));
            Assert.NotNull(item);
        }

        [Fact]
        public void Test_CompositePayloadConverter_Null_Roundtrip()
        {
            var converter = new CompositePayloadConverter();
            var p = new Payloads();
            Assert.True(converter.TrySerialize<string>(null, p));
            Assert.Single(p.Payloads_);
            Assert.True(converter.TryDeserialize(p, out string item));
            Assert.Null(item);
        }

        [Fact]
        public void Test_CompositePayloadConverter_Unnamed_Roundtrip()
        {
            var unnamed = new UnnamedContainerPayloadConverter();
            unnamed.InitDelegates(new []{new JsonPayloadConverter()});
            var instance = new CompositePayloadConverter(new IPayloadConverter[]
            {
                new VoidPayloadConverter(),
                new NullPayloadConverter(),
                new UnnamedContainerPayloadConverter(),
                new JsonPayloadConverter(),
            });
            var p = new Payloads();
            var data = new PayloadContainers.Unnamed.InstanceBacked<string>(new[] { "hello" });
            Assert.True(instance.TrySerialize(data, p));
            Assert.NotEmpty(p.Payloads_);
            Assert.True(instance.TryDeserialize(p, out PayloadContainers.Unnamed.SerializedDataBacked cl));
            Assert.NotEmpty(cl);
            Assert.True(cl.TryGetValue(0, out string val));
            Assert.Equal("hello", val);
        }

        [Fact]
        public void Test_CompositePayloadConverter_Catchall_Roundtrip()
        {
            var converter = new CompositePayloadConverter();
            var p = new Payloads();
            Assert.True(converter.TrySerialize(new IPayload.Void(), p));
            Assert.Empty(p.Payloads_);
            Assert.True(converter.TryDeserialize(p, out IPayload.Void item));
            Assert.NotNull(item);
        }
    }
}