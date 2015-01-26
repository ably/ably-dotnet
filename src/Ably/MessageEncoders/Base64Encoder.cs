using System;

namespace Ably.MessageEncoders
{
    internal class Base64Encoder : MessageEncoder
    {
        public override string EncodingName
        {
            get { return "base64"; }
        }

        public override void Decode(IEncodedMessage payload, ChannelOptions options)
        {
            if (CurrentEncodingIs(payload, EncodingName) && payload.Data is string)
            {
                payload.Data = ((string) payload.Data).FromBase64();
                RemoveCurrentEncodingPart(payload);
            }
        }

        public override void Encode(IEncodedMessage payload, ChannelOptions options)
        {
            var data = payload.Data;
            if (IsEmpty(data))
                return;

            var bytes = data as byte[];
            if (bytes != null && Protocol == Protocol.Json)
            {
                payload.Data = bytes.ToBase64();
                AddEncoding(payload, EncodingName);
            }
        }

        public Base64Encoder(Protocol protocol)
            : base(protocol)
        {
        }
    }
}