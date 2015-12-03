using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ably.CustomSerialisers
{
    public class DateTimeOffsetMilisecondJsonConverter : DateTimeOffsetJsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var date = (DateTimeOffset)value;
            writer.WriteValue(date.ToUnixTimeInMilliseconds());
        }
    }

    public class DateTimeOffsetJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var date = (DateTimeOffset)value;
            writer.WriteValue(date.ToUnixTime());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            if (token.Type == JTokenType.Integer)
            {
                var value = (long)token;
                if (ValueIsInMilliseconds(value))
                    return value.FromUnixTimeInMilliseconds();
                return value.FromUnixTime();
            }
            return DateTimeOffset.MinValue;
        }

        private bool ValueIsInMilliseconds(long value)
        {
            long num = (long)(value * 1000 + (value >= 0.0 ? 0.5 : -0.5));
            return num <= -315537897600000L || num >= 315537897600000L;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?);
        }
    }
}