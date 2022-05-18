using Temporal.Api.Common.V1;
using Temporal.Serialization;
using Temporal.TestUtil;
using Xunit;

namespace Temporal.Sdk.Common.Tests.Serialization
{
    public class TestProtobufPayloadConverter
    {
        [Fact]
        public void Test_ProtobufPayloadConverter_IMessage_Roundtrip()
        {
            WorkflowExecution wf = new() { WorkflowId = "test", RunId = "tset" };
            ProtobufPayloadConverter instance = new();
            Payloads p = new();
            Assert.True(instance.TrySerialize(wf, p));
            Assert.True(instance.TryDeserialize(p, out WorkflowExecution actual));
            Assert.NotNull(actual);
            Assert.Equal(wf.WorkflowId, actual.WorkflowId);
            Assert.Equal(wf.RunId, actual.RunId);
        }

        [Fact]
        [Trait("Category", "Common")]
        public void Test_ProtobufPayloadConverter_POCO_Roundtrip_Failure()
        {
            ProtobufPayloadConverter instance = new();
            Payloads p = new();
            Assert.False(instance.TrySerialize(SerializableClass.Default, p));
            Assert.False(instance.TryDeserialize(p, out SerializableClass actual));
            Assert.Null(actual);
        }
    }
}