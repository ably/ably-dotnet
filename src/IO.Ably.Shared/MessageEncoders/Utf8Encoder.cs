using IO.Ably.Rest;

namespace IO.Ably.MessageEncoders
{
    internal class Utf8Encoder : MessageEncoder
    {
        public Utf8Encoder()
            : base()
        {
        }

        public override string EncodingName => "utf-8";

        public override Result Encode(IMessage payload, EncodingDecodingContext context)
        {
            return Result.Ok();
        }

        public override Result Decode(IMessage payload, EncodingDecodingContext context)
        {
            // Assume all the other steps will always work with Utf8
            if (CurrentEncodingIs(payload, EncodingName))
            {
                payload.Data = (payload.Data as byte[]).GetText();
                RemoveCurrentEncodingPart(payload);
            }

            return Result.Ok();
        }
    }
}
