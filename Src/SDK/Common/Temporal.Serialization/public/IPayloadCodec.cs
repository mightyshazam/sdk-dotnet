using Temporal.Api.Common.V1;

namespace Temporal.Serialization
{
    public interface IPayloadCodec
    {
        Payloads Decode(Payloads data);
        Payloads Encode(Payloads data);
    }
}
