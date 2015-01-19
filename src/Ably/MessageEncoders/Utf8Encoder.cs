namespace Ably.MessageEncoders
{
    internal class Utf8Encoder : MessageEncoder
    {
        public Utf8Encoder(Protocol protocol)
            : base(protocol)
        {
        }

        public override string EncodingName
        {
            get { return "utf-8"; }
        }

        public override void Encode(MessagePayload payload, ChannelOptions options)
        {

        }

        public override void Decode(MessagePayload payload, ChannelOptions options)
        {
            //Assume all the other steps will always work with Utf8
            if (CurrentEncodingIs(payload, EncodingName))
            {
                payload.data = (payload.data as byte[]).GetText();
                RemoveCurrentEncodingPart(payload);
            }
        }
    }
}