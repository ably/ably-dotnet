using System;
using System.Linq;
using IO.Ably.Rest;

namespace IO.Ably.MessageEncoders
{
    internal abstract class MessageEncoder
    {
        protected readonly Protocol Protocol;

        protected MessageEncoder(Protocol protocol)
        {
            Protocol = protocol;
        }

        public abstract string EncodingName { get; }
        public abstract Result Encode(IMessage payload, ChannelOptions options);
        public abstract Result Decode(IMessage payload, ChannelOptions options);

        public bool IsEmpty(object data)
        {
            return data == null || (data is string && ((string)data).IsEmpty());
        }

        public void AddEncoding(IMessage payload, string encoding = null)
        {
            var encodingToAdd = encoding ?? EncodingName;
            if (payload.encoding.IsEmpty())
                payload.encoding = encodingToAdd;
            else
            {
                payload.encoding += "/" + encodingToAdd;
            }
        }

        public bool CurrentEncodingIs(IMessage payload, string encoding)
        {
            return payload.encoding.IsNotEmpty() && payload.encoding.EndsWith(encoding, StringComparison.CurrentCultureIgnoreCase);
        }

        public string GetCurrentEncoding(IMessage payload)
        {
            if (payload.encoding.IsEmpty())
                return "";

            return payload.encoding.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries).Last();
        }

        protected void RemoveCurrentEncodingPart(IMessage payload)
        {
            if (payload.encoding.IsEmpty())
                return;

            var encodings = payload.encoding.Split(new[] { '/' });
            payload.encoding = string.Join("/", encodings.Take(encodings.Length - 1));
        }
    }
}