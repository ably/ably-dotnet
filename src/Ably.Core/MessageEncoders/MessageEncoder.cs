using System;
using System.Linq;
using Ably.Rest;

namespace Ably.MessageEncoders
{
    internal abstract class MessageEncoder
    {
        protected readonly Protocol Protocol;

        protected MessageEncoder(Protocol protocol)
        {
            Protocol = protocol;
        }

        public abstract string EncodingName { get; }
        public abstract void Encode(IEncodedMessage payload, ChannelOptions options);
        public abstract void Decode(IEncodedMessage payload, ChannelOptions options);

        public bool IsEmpty(object data)
        {
            return data == null || (data is string && ((string)data).IsEmpty());
        }

        public void AddEncoding(IEncodedMessage payload, string encoding = null)
        {
            var encodingToAdd = encoding ?? EncodingName;
            if (payload.encoding.IsEmpty())
                payload.encoding = encodingToAdd;
            else
            {
                payload.encoding += "/" + encodingToAdd;
            }
        }

        public bool CurrentEncodingIs(IEncodedMessage payload, string encoding)
        {
            return payload.encoding.IsNotEmpty() && payload.encoding.EndsWith(encoding, StringComparison.CurrentCultureIgnoreCase);
        }

        public string GetCurrentEncoding(IEncodedMessage payload)
        {
            if (payload.encoding.IsEmpty())
                return "";

            return payload.encoding.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries).Last();
        }

        protected void RemoveCurrentEncodingPart(IEncodedMessage payload)
        {
            if (payload.encoding.IsEmpty())
                return;

            var encodings = payload.encoding.Split(new[] { '/' });
            payload.encoding = string.Join("/", encodings.Take(encodings.Length - 1));
        }
    }
}