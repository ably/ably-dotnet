using System.Linq;
using FluentAssertions;
using IO.Ably.Tests.Shared.Helpers;
using IO.Ably.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.CustomSerializers
{
    public class MessageExtrasConverterTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly JsonSerializerSettings JsonSettings = JsonHelper.Settings;

        public MessageExtrasConverterTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        [Trait("spec ", "tm2i")]
        public void ShouldParse_MessageExtrasJson()
        {
            var json = @"
                        {
                            'random':'boo',
                            'delta': {
                                'From': '1',
                                'Format':'best'
                             }
                        }";
            var originalJToken = JToken.Parse(json);
            var messageExtras = JsonConvert.DeserializeObject<MessageExtras>(json, JsonSettings);

            messageExtras.Delta.Should().NotBeNull();
            messageExtras.Delta.From.Should().Be("1");
            messageExtras.Delta.Format.Should().Be("best");

            ((string)messageExtras.ToJson()["random"]).Should().Be("boo");

            var serialized = JsonConvert.SerializeObject(messageExtras, JsonSettings);
            var serializedJToken = JToken.Parse(serialized);

            JAssert.DeepEquals(serializedJToken, originalJToken, _testOutputHelper).Should().Be(true);
        }

        [Fact]
        [Trait("spec ", "tm2i")]
        public void ShouldParse_MessageExtrasJson_WithEmptyDelta()
        {
            var json = @"{
                            'random':'boo',
                            'foo':'fooValue',
                            'bar':'barValue',
                            'object' : {
                                'key1': 'value1',
                                'key2': 'value2'
                            }
                        }";
            var originalJToken = JToken.Parse(json);
            var messageExtras = JsonConvert.DeserializeObject<MessageExtras>(json, JsonSettings);
            ((string)messageExtras.ToJson()["random"]).Should().Be("boo");
            ((string)messageExtras.ToJson()["foo"]).Should().Be("fooValue");
            ((string)messageExtras.ToJson()["bar"]).Should().Be("barValue");
            ((string)messageExtras.ToJson()["object"]["key1"]).Should().Be("value1");
            ((string)messageExtras.ToJson()["object"]["key2"]).Should().Be("value2");

            var serialized = JsonConvert.SerializeObject(messageExtras, JsonSettings);
            var serializedJToken = JToken.Parse(serialized);
            JAssert.DeepEquals(serializedJToken, originalJToken, _testOutputHelper).Should().Be(true);
        }

        [Fact]
        [Trait("spec ", "tm2i")]
        public void ShouldParse_MessageExtrasJson_WithDelta()
        {
            var json = @"{
                            'delta': {
                                'From': '1',
                                'Format':'best'
                             }
                        }";
            var originalJToken = JToken.Parse(json);
            var messageExtras = JsonConvert.DeserializeObject<MessageExtras>(json, JsonSettings);

            messageExtras.Delta.Should().NotBeNull();
            messageExtras.Delta.From.Should().Be("1");
            messageExtras.Delta.Format.Should().Be("best");

            var serialized = JsonConvert.SerializeObject(messageExtras, JsonSettings);
            var serializedJToken = JToken.Parse(serialized);
            JAssert.DeepEquals(serializedJToken, originalJToken, _testOutputHelper).Should().Be(true);
        }

        [Fact]
        [Trait("spec ", "tm2i")]
        public void ShouldParse_Message_WithNullMessageExtras()
        {
            var json = @"{
                            'id':'UniqueId',
                            'clientId':'clientId',
                            'connectionId':'connectionId',
                            'name':'connectionName',
                            'data':'data',
                            'encoding':'encoding',
                            'extras': null
                        }";

            var messageObject = JsonConvert.DeserializeObject<Message>(json, JsonSettings);
            messageObject.Extras.Should().BeNull();
            var serialized = JsonConvert.SerializeObject(messageObject, JsonSettings);
            var serializedJToken = JToken.Parse(serialized);
            serializedJToken.Contains("extras").Should().Be(false);
        }

        [Fact]
        [Trait("spec ", "tm2i")]
        public void ShouldParse_Message_WithArbitraryMessageExtras()
        {
            var json = @"{
                            'id':'UniqueId',
                            'clientId':'clientId',
                            'connectionId':'connectionId',
                            'name':'connectionName',
                            'data':'data',
                            'extras': 'extraData',
                            'encoding':'encoding'
                        }";

            var messageObject = JsonConvert.DeserializeObject<Message>(json, JsonSettings);
            messageObject.Extras.Delta.Should().BeNull();
            messageObject.Extras.ToJson().ToString().Should().Be("extraData");
            var serialized = JsonConvert.SerializeObject(messageObject, JsonSettings);
            var serializedJToken = JToken.Parse(serialized);
            serializedJToken.Contains("extras").Should().Be(false);
        }
    }
}
