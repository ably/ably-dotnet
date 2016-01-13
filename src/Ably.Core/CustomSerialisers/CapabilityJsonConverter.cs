using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ably.CustomSerialisers
{
    public class CapabilityJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue((value as Capability).ToJson());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var capToken = JToken.Load(reader);
            return new Capability(capToken.ToString());
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Capability);
        }
    }
}