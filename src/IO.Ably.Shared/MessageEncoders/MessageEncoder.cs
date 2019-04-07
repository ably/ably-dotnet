using System;
using System.Linq;
using IO.Ably.Rest;

namespace IO.Ably.MessageEncoders
{
    internal abstract class MessageEncoder
    {
        internal ILogger Logger { get; set; }

        public abstract string EncodingName { get; }

        public abstract Result Encode(IMessage payload, ChannelOptions options);

        public abstract Result Decode(IMessage payload, ChannelOptions options);

        public bool IsEmpty(object data)
        {
            return data == null || (data is string s && s.IsEmpty());
        }

        public void AddEncoding(IMessage payload, string encoding = null)
        {
            var encodingToAdd = encoding ?? EncodingName;
            if (payload.Encoding.IsEmpty())
            {
                payload.Encoding = encodingToAdd;
            }
            else
            {
                payload.Encoding += "/" + encodingToAdd;
            }
        }

        public bool CurrentEncodingIs(IMessage payload, string encoding)
        {
            return payload.Encoding.IsNotEmpty() && payload.Encoding.EndsWith(encoding, StringComparison.CurrentCultureIgnoreCase);
        }

        public string GetCurrentEncoding(IMessage payload)
        {
            if (payload.Encoding.IsEmpty())
            {
                return string.Empty;
            }

            return payload.Encoding.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Last();
        }

        protected void RemoveCurrentEncodingPart(IMessage payload)
        {
            if (payload.Encoding.IsEmpty())
            {
                return;
            }

            var encodings = payload.Encoding.Split(new[] { '/' });
            payload.Encoding = string.Join("/", encodings.Take(encodings.Length - 1));
        }
    }
}
