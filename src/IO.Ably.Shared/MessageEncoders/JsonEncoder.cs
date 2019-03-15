using System;

using IO.Ably;
using IO.Ably.Rest;

using Newtonsoft.Json;

namespace IO.Ably.MessageEncoders
{
    internal class JsonEncoder : MessageEncoder
    {
        public override string EncodingName => "json";

        public override Result Decode(IMessage payload, ChannelOptions options)
        {
            Logger = options?.Logger ?? IO.Ably.DefaultLogger.LoggerInstance;

            if (IsEmpty(payload.Data) || !CurrentEncodingIs(payload, EncodingName))
            {
                return Result.Ok();
            }

            try
            {
                payload.Data = JsonHelper.Deserialize(payload.Data as string);
            }
            catch (Exception ex)
            {
                Logger.Error($"Invalid Json data: '{payload.Data}'", ex);
                return Result.Fail(new ErrorInfo($"Invalid Json data: '{payload.Data}'"));
            }

            RemoveCurrentEncodingPart(payload);
            return Result.Ok();
        }

        public override Result Encode(IMessage payload, ChannelOptions options)
        {
            if (IsEmpty(payload.Data))
            {
                return Result.Ok();
            }

            if (NeedsJsonEncoding(payload))
            {
                payload.Data = JsonHelper.Serialize(payload.Data);
                AddEncoding(payload, EncodingName);
            }

            return Result.Ok();
        }

        public bool NeedsJsonEncoding(IMessage payload)
        {
            return payload.Data is string == false && payload.Data is byte[] == false;
        }

        public JsonEncoder()
            : base()
        {
        }
    }
}
