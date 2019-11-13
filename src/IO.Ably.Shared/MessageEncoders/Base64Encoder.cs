using System;
using IO.Ably.Rest;

namespace IO.Ably.MessageEncoders
{
    internal class Base64Encoder : MessageEncoder
    {
        public const string EncodingNameStr = "base64";

        public override string EncodingName => EncodingNameStr;

        public override Result<ProcessedPayload> Decode(IPayload payload, EncodingDecodingContext context)
        {
            if (CurrentEncodingIs(payload, EncodingName) && payload.Data is string data)
            {
                return Result.Ok(new ProcessedPayload()
                {
                    Data = data.FromBase64(),
                    Encoding = RemoveCurrentEncodingPart(payload),
                });
            }

            return Result.Ok(new ProcessedPayload(payload));
        }

        public override bool CanProcess(string currentEncoding)
            => currentEncoding.EqualsTo(EncodingNameStr);

        public override Result<ProcessedPayload> Encode(IPayload payload, EncodingDecodingContext context)
        {
            var data = payload.Data;
            if (IsEmpty(data))
            {
                return Result.Ok(new ProcessedPayload(payload));
            }

            if (data is byte[] bytes)
            {
                return Result.Ok(new ProcessedPayload()
                {
                    Data = bytes.ToBase64(),
                    Encoding = AddEncoding(payload, EncodingName),
                });
            }

            return Result.Ok(new ProcessedPayload(payload));
        }
    }
}
