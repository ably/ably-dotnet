using System;

namespace IO.Ably.MessageEncoders
{
    internal class AblyCodecEncoder : MessageEncoder
    {
        private readonly IAblyCodec _codecEncoder;
        private readonly ILogger _logger;

        public AblyCodecEncoder(string encodingName, IAblyCodec codecEncoder, ILogger logger)
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
            _logger = logger;
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
            try
            {
                _logger.Debug($"CustomCodec - EncodingName: {EncodingName}. AblyCodec: {_codecEncoder.GetType().Name}");
                var decodedPayload = _codecEncoder.Decode(payload.Data, context);
                return Result.Ok(new ProcessedPayload()
                {
                    Data = decodedPayload,
                    Encoding = RemoveCurrentEncodingPart(payload),
                });
            }
            catch (AblyException ablyEx)
            {
                ablyEx.ErrorInfo.InnerException = ablyEx;
                return Result.Fail<ProcessedPayload>(ablyEx.ErrorInfo);
            }
            catch (Exception e)
            {
                return Result.Fail<ProcessedPayload>(
                    new ErrorInfo($"Error decoding Payload with encoding: {payload.Encoding} using AblyCodec {_codecEncoder.GetType().Name}")
                        { InnerException = e });
            }
        }
    }
}
