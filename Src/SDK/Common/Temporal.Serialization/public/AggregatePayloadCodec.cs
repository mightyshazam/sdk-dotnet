using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Candidly.Util;

using Temporal.Common.Payloads;

using SerializedPayload = Temporal.Api.Common.V1.Payload;
using SerializedPayloads = Temporal.Api.Common.V1.Payloads;

namespace Temporal.Serialization
{
    public sealed class AggregatePayloadCodec : IPayloadCodec
    {
        public static IList<IPayloadCodec> CreateDefaultCodecs()
        {
            List<IPayloadCodec> converters = new List<IPayloadCodec>(capacity: 0)
            {
                // No default codecs
            };

            return converters;
        }

        private readonly List<IPayloadCodec> _codecs;

        public AggregatePayloadCodec()
            : this(CreateDefaultCodecs())
        {
        }

        public AggregatePayloadCodec(IEnumerable<IPayloadCodec> codecs)
        {
            _codecs = SerializationUtil.EnsureIsList(codecs);
        }

        public IReadOnlyList<IPayloadCodec> Codecs { get { return _codecs; } }

        public async Task<SerializedPayloads> EncodeAsync(SerializedPayloads data, CancellationToken cancelToken)
        {
            if (data == null)
            {
                return null;
            }

            for (int c = 0; c < _codecs.Count; c++)
            {
                if (_codecs[c] != null)
                {
                    data = await _codecs[c].EncodeAsync(data, cancelToken);
                }
            }

            return data;
        }

        public async Task<SerializedPayloads> DecodeAsync(SerializedPayloads data, CancellationToken cancelToken)
        {
            Validate.NotNull(data);

            // Since all codecs apply, must traverse in the reverse order of encoding.
            for (int c = _codecs.Count - 1; c >= 0; c--)
            {
                if (_codecs[c] != null)
                {
                    data = await _codecs[c].DecodeAsync(data, cancelToken);
                }
            }

            return data;
        }
    }
}