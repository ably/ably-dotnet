using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IO.Ably.CustomSerialisers
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Newtonsoft.Json converter. Used internally.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "CS1591:Missing XML comment for publicly visible type or member", Justification = "Newtonsoft.Json converter. Used internally.")]
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
