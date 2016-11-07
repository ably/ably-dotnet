using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using IO.Ably.CustomSerialisers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IO.Ably
{
    internal static class JsonHelper
    {
        internal static JsonSerializerSettings GetJsonSettings()
        {
            JsonSerializerSettings res = new JsonSerializerSettings();
            res.Converters = new List<JsonConverter>()
            {
                new DateTimeOffsetJsonConverter(),
                new CapabilityJsonConverter(),
                new TimeSpanJsonConverter()
            };
            res.DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind;
            res.NullValueHandling = NullValueHandling.Ignore;
            return res;
        }

        internal static JsonSerializer GetSerializer()
        {
            var settings = GetJsonSettings();

            return JsonSerializer.CreateDefault(settings);
        }

        private static JsonSerializerSettings _settings;
        internal static JsonSerializerSettings Settings => _settings ?? (_settings = GetJsonSettings());

        internal static string Serialize(object obj)
        {
            if(obj == null)
                throw new ArgumentNullException(nameof(obj), "Cannot serialize null object");

            return SerializeObject(obj, obj.GetType());
        }

        internal static T Deserialize<T>(string json)
        {
            return (T) DeserializeObject(json, typeof(T));
        }

        internal static object Deserialize(string json)
        {
            return DeserializeObject(json, null);
        }

        internal static T DeserializeObject<T>(JObject obj)
        {
            return obj.ToObject<T>(GetSerializer());
        }

        private static string SerializeObject(object value, Type type)
        {
            var jsonSerializer = GetSerializer();
            StringWriter stringWriter = new StringWriter(new StringBuilder(256), CultureInfo.InvariantCulture);
            using (JsonTextWriter jsonTextWriter = new JsonTextWriter(stringWriter))
            {
                jsonTextWriter.Formatting = jsonSerializer.Formatting;
                jsonSerializer.Serialize(jsonTextWriter, value, type);
            }
            return stringWriter.ToString();
        }

        internal static object DeserializeObject(string value, Type type)
        {
            JsonSerializer jsonSerializer = GetSerializer();
            jsonSerializer.CheckAdditionalContent = true;

            using (JsonTextReader jsonTextReader = new JsonTextReader(new StringReader(value)))
                return jsonSerializer.Deserialize(jsonTextReader, type);
        }
    }
}