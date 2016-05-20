using IO.Ably.Rest;

namespace IO.Ably.MessageEncoders
{
    internal class Utf8Encoder : MessageEncoder
    {
        public Utf8Encoder(Protocol protocol)
            : base(protocol)
        {
        }

        public override string EncodingName => "utf-8";

        public override Result Encode(IMessage payload, ChannelOptions options)
        {
            return Result.Ok();
        }

        public override Result Decode(IMessage payload, ChannelOptions options)
        {
            //Assume all the other steps will always work with Utf8
            if (CurrentEncodingIs(payload, EncodingName))
            {
                payload.data = (payload.data as byte[]).GetText();
                RemoveCurrentEncodingPart(payload);
            }
            return Result.Ok();
        }
    }
}