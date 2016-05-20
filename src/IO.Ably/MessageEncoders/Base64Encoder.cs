using System;
using IO.Ably.Rest;

namespace IO.Ably.MessageEncoders
{
    internal class Base64Encoder : MessageEncoder
    {
        public override string EncodingName => "base64";

        public override Result Decode(IMessage payload, ChannelOptions options)
        {
            if (CurrentEncodingIs(payload, EncodingName) && payload.data is string)
            {
                payload.data = ((string) payload.data).FromBase64();
                RemoveCurrentEncodingPart(payload);
            }
            return Result.Ok();
        }

        public override Result Encode(IMessage payload, ChannelOptions options)
        {
            var data = payload.data;
            if (IsEmpty(data))
                return Result.Ok();

            var bytes = data as byte[];
            if (bytes != null && Protocol == Protocol.Json)
            {
                payload.data = bytes.ToBase64();
                AddEncoding(payload, EncodingName);
            }
            return Result.Ok();
        }

        public Base64Encoder(Protocol protocol)
            : base(protocol)
        {
        }
    }
}