using System;
using Google.Protobuf;
using Temporal.Api.Common.V1;
using Temporal.Common.Payloads;
using Temporal.Serialization;
using Xunit;

namespace Temporal.Sdk.Common.Tests.Serialization
{
    public class TestUnnamedContainerPayloadConverter
    {
        [Fact]
        public void Test_UnnamedContainerPayloadConverter_TrySerialize_String()
        {
            var instance = new UnnamedContainerPayloadConverter();
            var p = new Payloads();
            Assert.False(instance.TrySerialize(String.Empty, p));
            Assert.Empty(p.Payloads_);
        }

        [Fact]
        public void Test_UnnamedContainerPayloadConverter_TrySerialize_Null()
        {
            var instance = new UnnamedContainerPayloadConverter();
            var p = new Payloads();
            Assert.False(instance.TrySerialize<string>(null, p));
            Assert.Empty(p.Payloads_);
        }

        [Fact]
        public void Test_UnnamedContainerPayloadConverter_TrySerialize_Unnamed_SerializedDataBacked()
        {
            var instance = new UnnamedContainerPayloadConverter();
            instance.InitDelegates(new[]{new JsonPayloadConverter()});
            var p = new Payloads();
            var data = new PayloadContainers.Unnamed.SerializedDataBacked(new Payloads
            {
                Payloads_ =
                {
                    new Payload
                    {
                        Data = ByteString.CopyFromUtf8("{\"name\": \"test\", \"value\": 2}"),
                    }
                }
            }, new JsonPayloadConverter());
            Assert.True(instance.TrySerialize(data, p));
            Assert.NotEmpty(p.Payloads_);
            Assert.True(instance.TryDeserialize(p, out PayloadContainers.Unnamed.SerializedDataBacked cl));
            Assert.NotNull(cl);
            SerializableClass deserializedData = cl.GetValue<SerializableClass>(0);
            Assert.Equal("test", deserializedData.Name);
            Assert.Equal(2, deserializedData.Value);
        }

        [Fact]
        public void Test_UnnamedContainerPayloadConverter_TrySerialize_Unnamed_InstanceBacked()
        {
            var instance = new UnnamedContainerPayloadConverter();
            instance.InitDelegates(new[]{new JsonPayloadConverter()});
            var p = new Payloads();
            var data = new PayloadContainers.Unnamed.InstanceBacked<string>(new[]{"hello"});
            Assert.True(instance.TrySerialize(data, p));
            Assert.NotEmpty(p.Payloads_);
            Assert.False(instance.TryDeserialize(p, out PayloadContainers.Unnamed.InstanceBacked<string> _));
            Assert.True(instance.TryDeserialize(p, out PayloadContainers.Unnamed.SerializedDataBacked cl));
            Assert.NotEmpty(cl);
            Assert.True(cl.TryGetValue(0, out string val));
            Assert.Equal("hello", val);
        }

        [Fact]
        public void Test_UnnamedContainerPayloadConverter_TrySerialize_Unnamed_Empty()
        {
            var instance = new UnnamedContainerPayloadConverter();
            instance.InitDelegates(new[]{new JsonPayloadConverter()});
            var p = new Payloads();
            var data = new PayloadContainers.Unnamed.Empty();
            Assert.True(instance.TrySerialize(data, p));
            Assert.Empty(p.Payloads_);
        }
    }
}