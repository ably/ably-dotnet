using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IO.Ably.CustomSerialisers
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Newtonsoft.Json converter. Used internally.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "CS1591:Missing XML comment for publicly visible type or member", Justification = "Newtonsoft.Json converter. Used internally.")]
    public class DateTimeOffsetJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var date = (DateTimeOffset)value;
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

            return DateTimeOffset.MinValue;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?);
        }
    }
}
