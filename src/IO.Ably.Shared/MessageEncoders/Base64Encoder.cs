using System;
using IO.Ably.Rest;

namespace IO.Ably.MessageEncoders
{
    internal class Base64Encoder : MessageEncoder
    {
        public override string EncodingName => "base64";

        public override Result Decode(IMessage payload, ChannelOptions options)
        {
            if (CurrentEncodingIs(payload, EncodingName) && payload.Data is string)
            {
                payload.Data = ((string) payload.Data).FromBase64();
                RemoveCurrentEncodingPart(payload);
            }
            return Result.Ok();
        }

        public override Result Encode(IMessage payload, ChannelOptions options)
        {
            var data = payload.Data;
            if (IsEmpty(data))
            {
                return Result.Ok();
            }

            var bytes = data as byte[];
            if (bytes != null && Protocol == Protocol.Json)
            {
                payload.Data = bytes.ToBase64();
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