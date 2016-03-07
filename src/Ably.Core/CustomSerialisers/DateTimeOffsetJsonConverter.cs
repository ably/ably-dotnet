using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IO.Ably.CustomSerialisers
{
    public class DateTimeOffsetJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var date = (DateTime)value;
            writer.WriteValue(date.ToUnixTimeInMilliseconds());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            if (token.Type == JTokenType.Integer)
            {
                var value = (long)token;
                return value.FromUnixTimeInMilliseconds();
            }
            return DateTime.MinValue;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime) || objectType == typeof(DateTime?);
        }
    }
}