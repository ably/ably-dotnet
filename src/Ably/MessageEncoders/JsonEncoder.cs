using System;
using Newtonsoft.Json;

namespace Ably.MessageEncoders
{
    internal class JsonEncoder : MessageEncoder
    {
        public override string EncodingName
        {
            get { return "json"; }
        }

        public override void Decode(IEncodedMessage payload, ChannelOptions options)
        {
            if (IsEmpty(payload.Data) || CurrentEncodingIs(payload, EncodingName) == false) return;

            try
            {
                payload.Data = JsonConvert.DeserializeObject(payload.Data as string);
            }
            catch (Exception ex)
            {
                throw new AblyException(new ErrorInfo(string.Format("Invalid Json data: '{0}'", payload.Data), 50000), ex);
            }
            RemoveCurrentEncodingPart(payload);
        }

        public override void Encode(IEncodedMessage payload, ChannelOptions options)
        {
            if (IsEmpty(payload.Data)) return;

            if (NeedsJsonEncoding(payload))
            {
                payload.Data = JsonConvert.SerializeObject(payload.Data);
                AddEncoding(payload, EncodingName);
            }
        }


        public bool NeedsJsonEncoding(IEncodedMessage payload)
        {
            return payload.Data is string == false && payload.Data is byte[] == false;
        }

        public JsonEncoder(Protocol protocol)
            : base(protocol)
        {
        }
    }
}
