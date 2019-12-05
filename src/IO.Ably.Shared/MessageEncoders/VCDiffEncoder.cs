using System;
using IO.Ably.Diff;

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

        public override Result<ProcessedPayload> Encode(IPayload payload, EncodingDecodingContext context)
        {
            return Result.Ok(new ProcessedPayload(payload));
        }

        public override Result<ProcessedPayload> Decode(IPayload payload, EncodingDecodingContext context)
        {
            var logger = context.ChannelOptions?.Logger ?? DefaultLogger.LoggerInstance;
            if (payload == null)
            {
                return Result.Ok(new ProcessedPayload());
            }

            try
            {
                var payloadBytes = DataHelpers.ConvertToByteArray(payload.Data);

                var result = DeltaDecoder.ApplyDelta(context.PreviousPayload, payloadBytes);
                context.PreviousPayload = result.AsByteArray();
                context.PreviousPayloadEncoding = context.Encoding;
                context.Encoding = RemoveCurrentEncodingPart(payload);
                return Result.Ok(new ProcessedPayload()
                {
                    Data = result.AsByteArray(),
                    Encoding = RemoveCurrentEncodingPart(payload),
                });
            }
            catch (Exception e)
            {
                // TODO: Rethrow an error to indicate the vcdiff failed and things need to happen.
                var error =
                    $"Payload Encoding: {context.PreviousPayloadEncoding}. Payload: {context.PreviousPayload.Length} bytes";
                logger.Error("Error decoding vcdiff message: " + error, e);

                // TODO: Specify the correct error codes
                return Result.Fail<ProcessedPayload>(new ErrorInfo("Failed to decode vcdiff message"));
            }
        }
    }
}
