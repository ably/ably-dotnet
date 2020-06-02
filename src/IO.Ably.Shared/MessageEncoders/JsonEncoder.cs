﻿using System;

using IO.Ably;
using IO.Ably.Rest;

using Newtonsoft.Json;

namespace IO.Ably.MessageEncoders
{
    internal class JsonEncoder : MessageEncoder
    {
        private const string EncodingNameStr = "json";

        public override string EncodingName => EncodingNameStr;

        public override Result<ProcessedPayload> Decode(IPayload payload, DecodingContext context)
        {
            var options = context.ChannelOptions;
            var logger = options?.Logger ?? DefaultLogger.LoggerInstance;

            if (IsEmpty(payload.Data) || !CurrentEncodingIs(payload, EncodingName))
            {
                return Result.Ok(new ProcessedPayload(payload));
            }

            try
            {
                return Result.Ok(new ProcessedPayload()
                {
                    Data = JsonHelper.Deserialize(payload.Data as string),
                    Encoding = RemoveCurrentEncodingPart(payload),
                });
            }
            catch (Exception ex)
            {
                logger.Error($"Invalid Json data: '{payload.Data}'", ex);
                return Result.Fail<ProcessedPayload>(new ErrorInfo($"Invalid Json data: '{payload.Data}'"));
            }
        }

        public override bool CanProcess(string currentEncoding)
        {
            return currentEncoding.EqualsTo(EncodingNameStr);
        }

        public override Result<ProcessedPayload> Encode(IPayload payload, DecodingContext context)
        {
            if (IsEmpty(payload.Data))
            {
                return Result.Ok(new ProcessedPayload(payload));
            }

            if (NeedsJsonEncoding(payload))
            {
                return Result.Ok(new ProcessedPayload()
                {
                    Data = JsonHelper.Serialize(payload.Data),
                    Encoding = AddEncoding(payload, EncodingName),
                });
            }

            return Result.Ok(new ProcessedPayload(payload));
        }

        public bool NeedsJsonEncoding(IPayload payload)
        {
            return payload.Data is string == false && payload.Data is byte[] == false;
        }

        public JsonEncoder()
            : base()
        {
        }
    }
}
