using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ably
{
    public class Message
    {
        public string Name { get; set; }
        public string ChannelId { get; set; }
        public object Data { get; set; }

        public bool IsBinaryMessage
        {
            get
            {
                return Data is byte[];
            }
        }

        public T Value<T>()
        {
            if (IsBinaryMessage)
            {
                if (typeof(T) == typeof(byte[]))
                    return (T)Data;
                else
                    throw new InvalidOperationException(String.Format("Current message contains binary data which cannot be converted to {0}", typeof(T)));
            }

            JToken token = Data as JToken;
            if (token.Type == JTokenType.Object)
                return token.ToObject<T>();
            return token.Value<T>();
        }
        public DateTimeOffset TimeStamp { get; set; }
    }
}
