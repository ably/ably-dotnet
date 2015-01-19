using System;

namespace Ably.MessageEncoders
{
    internal class Base64Encoder : MessageEncoder
    {
        public override string EncodingName
        {
            get { return "base64"; }
        }

        public override void Decode(MessagePayload payload, ChannelOptions options)
        {
            if (CurrentEncodingIs(payload, EncodingName) && payload.data is string)
            {
                payload.data = ((string) payload.data).FromBase64();
                RemoveCurrentEncodingPart(payload);
            }
        }

        public override void Encode(MessagePayload payload, ChannelOptions options)
        {
            var data = payload.data;
            if (IsEmpty(data))
                return;

            var bytes = data as byte[];
            if (bytes != null && Protocol == Protocol.Json)
            {
                payload.data = bytes.ToBase64();
                AddEncoding(payload, EncodingName);
            }
        }

        public Base64Encoder(Protocol protocol)
            : base(protocol)
        {
        }
    }
}