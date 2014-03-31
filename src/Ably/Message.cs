using Newtonsoft.Json.Linq;
using System;

namespace Ably
{
    public class Message
    {
        public string Name { get; set; }
        public string ChannelId { get; set; }
        public object Data { get; set; }
        public long Timestamp { get; set; }

        public bool IsBinaryMessage
        {
            get { return Data is byte[]; }
        }

        public T Value<T>()
        {
            object value = Value(typeof(T));
            if (value == null)
                return default(T);
            return (T)value;
        }

        public object Value(Type type)
        {
            if (IsBinaryMessage)
            {
                if (type == typeof(byte[]))
                    return Data;
                throw new InvalidOperationException(
                    String.Format("Current message contains binary data which cannot be converted to {0}", type));
            }

            if (Data == null)
                return null;

            if (Data is JToken)
            {
                return ((JToken)Data).ToObject(type);
            }

            if (typeof(IConvertible).IsAssignableFrom(type))
            {
                return Convert.ChangeType(Data, type);
            }
            return Data;
        }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
