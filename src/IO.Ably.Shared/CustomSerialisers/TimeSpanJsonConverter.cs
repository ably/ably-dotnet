using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IO.Ably.CustomSerialisers
{
    public class TimeSpanJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var timeSpan = (TimeSpan?)value;
            if (timeSpan.HasValue)
                writer.WriteValue((long)timeSpan.Value.TotalMilliseconds);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            if (token.Type == JTokenType.Integer)
            {
                var value = (long)token;
                return TimeSpan.FromMilliseconds(value);
            }

            if (token.Type == JTokenType.Float)
            {
                var value = (float)token;
                return TimeSpan.FromMilliseconds(value);
            }

            if (token.Type == JTokenType.String)
            {
                TimeSpan result;
                if(TimeSpan.TryParse((string)token, out result))
                {
                    return result;
                }
            }

            return null;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TimeSpan) || objectType == typeof(TimeSpan?);
        }
    }
}