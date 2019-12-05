using System;

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
            var logger = context?.ChannelOptions?.Logger ?? DefaultLogger.LoggerInstance;
            return Result.Ok(new ProcessedPayload(payload));

            // TODO: Indicate terminal failure
//            try
//            {
//                logger.Debug($"CustomCodec - EncodingName: {EncodingName}. AblyCodec: {_codecEncoder.GetType().Name}");
//                var decodedPayload = _codecEncoder.Decode(payload.Data, context);
//                return Result.Ok(new ProcessedPayload()
//                {
//                    Data = decodedPayload,
//                    Encoding = RemoveCurrentEncodingPart(payload),
//                });
//            }
//            catch (AblyException ablyEx)
//            {
//                ablyEx.ErrorInfo.InnerException = ablyEx;
//                return Result.Fail<ProcessedPayload>(ablyEx.ErrorInfo);
//            }
//            catch (Exception e)
//            {
//                return Result.Fail<ProcessedPayload>(
//                    new ErrorInfo($"Error decoding Payload with encoding: {payload.Encoding} using AblyCodec {_codecEncoder.GetType().Name}")
//                        { InnerException = e });
//            }
        }
    }
}
