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

        public override void Decode(IMessage payload, ChannelOptions options)
        {
            if (IsEmpty(payload.data) || CurrentEncodingIs(payload, EncodingName) == false) return;

            try
            {
                payload.data = JsonConvert.DeserializeObject(payload.data as string);
            }
            catch (Exception ex)
            {
                throw new AblyException(new ErrorInfo(string.Format("Invalid Json data: '{0}'", payload.data), 50000), ex);
            }
            RemoveCurrentEncodingPart(payload);
        }

        public override void Encode(IMessage payload, ChannelOptions options)
        {
            if (IsEmpty(payload.data)) return;

            if (NeedsJsonEncoding(payload))
            {
                payload.data = JsonConvert.SerializeObject(payload.data);
                AddEncoding(payload, EncodingName);
            }
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
