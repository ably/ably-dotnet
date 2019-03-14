using System;
using IO.Ably.Rest;

namespace IO.Ably.MessageEncoders
{
    internal class Base64Encoder : MessageEncoder
    {
        private readonly Protocol _protocol;

        public override string EncodingName => "base64";

        public override Result Decode(IMessage payload, ChannelOptions options)
        {
            if (CurrentEncodingIs(payload, EncodingName) && payload.Data is string data)
            {
                payload.Data = data.FromBase64();
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

            if (data is byte[] bytes)
            {
                payload.Data = bytes.ToBase64();
                AddEncoding(payload, EncodingName);
            }

            return Result.Ok();
        }

        public Base64Encoder()
            : base()
        {
        }
    }
}
