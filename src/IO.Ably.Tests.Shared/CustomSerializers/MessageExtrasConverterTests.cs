using System.Collections.Generic;
using FluentAssertions;
using IO.Ably.CustomSerialisers;
using IO.Ably.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace IO.Ably.Tests.DotNetCore20.CustomSerializers
{
    public class MessageExtrasConverterTests
    {
        public JsonSerializerSettings JsonSettings
        {
            get
            {
                JsonSerializerSettings res = new JsonSerializerSettings();
                res.Converters = new List<JsonConverter>()
                {
                    new MessageExtrasConverter(),
                };
                res.DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind;
                res.NullValueHandling = NullValueHandling.Ignore;
                res.ContractResolver = new CamelCasePropertyNamesContractResolver();
                return res;
            }
        }

        [Fact]
        [Trait("spec ", "tm2i")]
        public void Should_parse_MessageExtras_json_correctly()
        {
            var json = "{ \"random\":\"boo\", \"delta\":{ \"From\": \"1\", \"Format\":\"best\" } }";
            var originalJObject = JObject.Parse(json);
            var messageExtras = JsonConvert.DeserializeObject<MessageExtras>(json, JsonSettings);

            messageExtras.Delta.Should().NotBeNull();
            messageExtras.Delta.From.Should().Be("1");
            messageExtras.Delta.Format.Should().Be("best");
            ((string)messageExtras.ToJson()["random"]).Should().Be("boo");

            var serialized = JsonConvert.SerializeObject(messageExtras, JsonSettings);
            var serializedJObject = JObject.Parse(serialized);
            JToken.DeepEquals(serializedJObject, originalJObject).Should().BeTrue();
        }
    }
}
