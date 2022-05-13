using System;
using Google.Protobuf;
using Temporal.Api.Common.V1;
using Temporal.Serialization;
using Xunit;

namespace Temporal.Sdk.Common.Tests.Serialization
{
    public class TestNullPayloadConverter
    {
        [Fact]
        public void Test_NullPayloadConverter_TryDeserialize_Nullable_Type()
        {
            ByteString bs = null;
            var instance = new NullPayloadConverter();
            ByteString b = PayloadConverter.GetOrCreateBytes(
                NullPayloadConverter.PayloadMetadataEncodingValue,
                ref bs);
            var p = new Payload { Metadata = { { PayloadConverter.PayloadMetadataEncodingKey, b } } };
            Assert.True(instance.TryDeserialize(
                new Payloads { Payloads_ = { p }, },
                out string s));
            Assert.Null(s);
        }

        [Fact]
        public void Test_NullPayloadConverter_TryDeserialize_Nonnullable_Type()
        {
            var instance = new NullPayloadConverter();
            Assert.False(instance.TryDeserialize(
                new Payloads { Payloads_ = { new Payload() }, },
                out int _));
        }

        [Fact]
        public void Test_NullPayloadConverter_TryDeserialize_MultiplePayloads()
        {
            var instance = new NullPayloadConverter();
            Assert.False(instance.TryDeserialize(
                new Payloads { Payloads_ = { new Payload(), new Payload() }, },
                out string _));
        }

        [Fact]
        public void Test_NullPayloadConverter_TryDeserialize_NoPayload()
        {
            var instance = new NullPayloadConverter();
            Assert.False(instance.TryDeserialize(new Payloads(), out string _));
        }

        [Fact]
        public void Test_NullPayloadConverter_TrySerialize_Null()
        {
            var instance = new NullPayloadConverter();
            Payloads p = new Payloads();
            Assert.True(instance.TrySerialize<string>(null, p));
            Assert.NotEmpty(p.Payloads_);
        }

        [Fact]
        public void Test_NullPayloadConverter_TrySerialize_ValueType_Null()
        {
            var instance = new NullPayloadConverter();
            Payloads p = new Payloads();
            Assert.False(instance.TrySerialize(0, p));
            Assert.Empty(p.Payloads_);
        }

        [Fact]
        public void Test_NullPayloadConverter_TrySerialize_Not_Null()
        {
            var instance = new NullPayloadConverter();
            Payloads p = new Payloads();
            Assert.False(instance.TrySerialize<string>(String.Empty, p));
            Assert.Empty(p.Payloads_);
        }
    }
}