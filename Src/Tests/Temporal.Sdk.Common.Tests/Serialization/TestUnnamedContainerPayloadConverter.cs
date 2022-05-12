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
<<<<<<< Updated upstream
            UnnamedContainerPayloadConverter instance = new UnnamedContainerPayloadConverter();
            Payloads p = new Payloads();
=======
            var instance = new UnnamedContainerPayloadConverter();
            var p = new Payloads();
>>>>>>> Stashed changes
            Assert.False(instance.TrySerialize(String.Empty, p));
            Assert.Empty(p.Payloads_);
        }

        [Fact]
        public void Test_UnnamedContainerPayloadConverter_TrySerialize_Null()
        {
<<<<<<< Updated upstream
            UnnamedContainerPayloadConverter instance = new UnnamedContainerPayloadConverter();
            Payloads p = new Payloads();
=======
            var instance = new UnnamedContainerPayloadConverter();
            var p = new Payloads();
>>>>>>> Stashed changes
            Assert.False(instance.TrySerialize<string>(null, p));
            Assert.Empty(p.Payloads_);
        }

        [Fact]
        public void Test_UnnamedContainerPayloadConverter_TrySerialize_Unnamed_SerializedDataBacked()
        {
<<<<<<< Updated upstream
            UnnamedContainerPayloadConverter instance = new UnnamedContainerPayloadConverter();
            instance.InitDelegates(new[]{new JsonPayloadConverter()});
            Payloads p = new Payloads();
            PayloadContainers.Unnamed.SerializedDataBacked data = new PayloadContainers.Unnamed.SerializedDataBacked(new Payloads
            {
                Payloads_ =
                {
                    new Payload
                    {
                        Data = ByteString.CopyFromUtf8("{\"name\": \"test\", \"value\": 2}"),
                    }
                }
            }, new JsonPayloadConverter());
=======
            var p = new Payloads();
            var converter = new NewtonsoftJsonPayloadConverter();
            converter.Serialize(new SerializableClass { Name = "test", Value = 2 }, p);
            var instance = new UnnamedContainerPayloadConverter();
            instance.InitDelegates(new[]{converter});
            var data = new PayloadContainers.Unnamed.SerializedDataBacked(p, converter);
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
            UnnamedContainerPayloadConverter instance = new UnnamedContainerPayloadConverter();
            instance.InitDelegates(new[]{new JsonPayloadConverter()});
            Payloads p = new Payloads();
            PayloadContainers.Unnamed.InstanceBacked<string> data = new PayloadContainers.Unnamed.InstanceBacked<string>(new[]{"hello"});
=======
            var instance = new UnnamedContainerPayloadConverter();
            instance.InitDelegates(new[]{new JsonPayloadConverter()});
            var p = new Payloads();
            var data = new PayloadContainers.Unnamed.InstanceBacked<string>(new[]{"hello"});
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
            UnnamedContainerPayloadConverter instance = new UnnamedContainerPayloadConverter();
            instance.InitDelegates(new[]{new JsonPayloadConverter()});
            Payloads p = new Payloads();
            PayloadContainers.Unnamed.Empty data = new PayloadContainers.Unnamed.Empty();
=======
            var instance = new UnnamedContainerPayloadConverter();
            instance.InitDelegates(new[]{new JsonPayloadConverter()});
            var p = new Payloads();
            var data = new PayloadContainers.Unnamed.Empty();
>>>>>>> Stashed changes
            Assert.True(instance.TrySerialize(data, p));
            Assert.Empty(p.Payloads_);
        }
    }
}