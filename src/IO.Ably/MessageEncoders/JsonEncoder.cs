using System;
using IO.Ably.Rest;
using Newtonsoft.Json;

namespace IO.Ably.MessageEncoders
{
    internal class JsonEncoder : MessageEncoder
    {
        public override string EncodingName
        {
            get { return "json"; }
        }

        public override Result Decode(IMessage payload, ChannelOptions options)
        {
            if (IsEmpty(payload.data) || CurrentEncodingIs(payload, EncodingName) == false) return Result.Ok();

            try
            {
                payload.data = JsonConvert.DeserializeObject(payload.data as string);
            }
            catch (Exception ex)
            {
                Logger.Error($"Invalid Json data: '{payload.data}'", ex);
                return Result.Fail(new ErrorInfo($"Invalid Json data: '{payload.data}'"));
            }
            RemoveCurrentEncodingPart(payload);
            return Result.Ok();
        }

        public override Result Encode(IMessage payload, ChannelOptions options)
        {
            if (IsEmpty(payload.data)) return Result.Ok();

            if (NeedsJsonEncoding(payload))
            {
                payload.data = JsonConvert.SerializeObject(payload.data);
                AddEncoding(payload, EncodingName);
            }
            return Result.Ok();
        }

        public bool NeedsJsonEncoding(IMessage payload)
        {
            return payload.data is string == false && payload.data is byte[] == false;
        }

        public JsonEncoder(Protocol protocol)
            : base(protocol)
        {
        }
    }
}
