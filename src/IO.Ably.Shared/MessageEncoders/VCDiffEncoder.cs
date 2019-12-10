using System;
using IO.Ably.DeltaCodec;

namespace IO.Ably.MessageEncoders
{
    internal class VCDiffEncoder : MessageEncoder
    {
        public const string EncodingNameStr = "vcdiff";

        public override string EncodingName => EncodingNameStr;

        public VCDiffEncoder()
        {
        }

        public override bool CanProcess(string currentEncoding)
        {
            return currentEncoding.IsNotEmpty() && currentEncoding.StartsWith("vcdiff");
        }

        public override Result<ProcessedPayload> Encode(IPayload payload, DecodingContext context)
        {
            return Result.Ok(new ProcessedPayload(payload));
        }

        public override Result<ProcessedPayload> Decode(IPayload payload, DecodingContext context)
        {
            var logger = context.ChannelOptions?.Logger ?? DefaultLogger.LoggerInstance;
            if (payload == null)
            {
                return Result.Ok(new ProcessedPayload());
            }

            try
            {
                var payloadBytes = DataHelpers.ConvertToByteArray(payload.Data);

                var previousPayload = context.PreviousPayload.GetBytes();
                if (previousPayload is null)
                {
                    return Result.Fail<ProcessedPayload>(new VcdiffErrorInfo("Missing previous payload"));
                }

                var result = DeltaDecoder.ApplyDelta(previousPayload, payloadBytes);
                var nextEncoding = RemoveCurrentEncodingPart(payload);

                context.PreviousPayload = new PayloadCache(result.AsByteArray(), nextEncoding);
                return Result.Ok(new ProcessedPayload
                {
                    Data = result.AsByteArray(),
                    Encoding = RemoveCurrentEncodingPart(payload),
                });
            }
            catch (Exception ex)
            {
                var error =
                    $"Payload Encoding: {context.PreviousPayload?.Encoding}. Payload: {context.PreviousPayload?.GetBytes().Length} bytes";
                logger.Error("Error decoding vcdiff message: " + error, ex);

                return Result.Fail<ProcessedPayload>(new VcdiffErrorInfo("Failed to decode vcdiff message", ex));
            }
        }
    }
}
