using System;

namespace IO.Ably.MessageEncoders
{
    internal class AblyCodecEncoder : MessageEncoder
    {
        private readonly IAblyCodec _codecEncoder;

        public AblyCodecEncoder(string encodingName, IAblyCodec codecEncoder)
        {
            if (encodingName.IsEmpty())
            {
                throw new ArgumentException("Invalid encoding name");
            }

            if (codecEncoder is null)
            {
                throw new ArgumentException("Invalid codec encoder");
            }

            EncodingName = encodingName;
            _codecEncoder = codecEncoder;
        }

        public override string EncodingName { get; }

        public override bool CanProcess(string currentEncoding)
        {
            return currentEncoding.EqualsTo(EncodingName);
        }

        public override Result<ProcessedPayload> Encode(IPayload payload, EncodingDecodingContext context)
        {
            return Result.Ok(new ProcessedPayload(payload));
        }

        public override Result<ProcessedPayload> Decode(IPayload payload, EncodingDecodingContext context)
        {
            return Result.Ok(new ProcessedPayload(payload));
        }
    }
}
