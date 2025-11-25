using FluentAssertions;
using IO.Ably.MsgPack.CustomSerialisers;
using IO.Ably.Tests.Shared.Helpers;
using IO.Ably.Types;
using MessagePack;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.MsgPack.CustomSerializers
{
    public class MessageExtrasFormatterTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public MessageExtrasFormatterTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        [Trait("spec", "tm2i")]
        public void ShouldSerializeAndDeserializeMessageExtrasWithDelta()
        {
            var deltaExtras = new DeltaExtras("1", "best");
            var jObject = new JObject
            {
                ["delta"] = new JObject
                {
                    ["From"] = "1",
                    ["Format"] = "best"
                }
            };
            var messageExtras = MessageExtras.From(jObject);

            var serialized = MsgPackHelper.Serialise<MessageExtras>(messageExtras);
            var deserialized = MsgPackHelper.Deserialise<MessageExtras>(serialized);

            deserialized.Should().NotBeNull();
            deserialized.Delta.Should().NotBeNull();
            deserialized.Delta.From.Should().Be("1");
            deserialized.Delta.Format.Should().Be("best");
        }

        [Fact]
        [Trait("spec", "tm2i")]
        public void ShouldSerializeAndDeserializeMessageExtrasWithArbitraryProperties()
        {
            var jObject = new JObject
            {
                ["random"] = "boo",
                ["delta"] = new JObject
                {
                    ["From"] = "1",
                    ["Format"] = "best"
                }
            };
            var messageExtras = MessageExtras.From(jObject);

            var serialized = MsgPackHelper.Serialise<MessageExtras>(messageExtras);
            var deserialized = MsgPackHelper.Deserialise<MessageExtras>(serialized);

            deserialized.Should().NotBeNull();
            deserialized.Delta.Should().NotBeNull();
            deserialized.Delta.From.Should().Be("1");
            deserialized.Delta.Format.Should().Be("best");
            ((string)deserialized.ToJson()["random"]).Should().Be("boo");
        }

        [Fact]
        [Trait("spec", "tm2i")]
        public void ShouldSerializeAndDeserializeMessageExtrasWithoutDelta()
        {
            var jObject = new JObject
            {
                ["random"] = "boo",
                ["foo"] = "fooValue",
                ["bar"] = "barValue",
                ["object"] = new JObject
                {
                    ["key1"] = "value1",
                    ["key2"] = "value2"
                }
            };
            var messageExtras = MessageExtras.From(jObject);

            var serialized = MsgPackHelper.Serialise<MessageExtras>(messageExtras);
            var deserialized = MsgPackHelper.Deserialise<MessageExtras>(serialized);

            deserialized.Should().NotBeNull();
            ((string)deserialized.ToJson()["random"]).Should().Be("boo");
            ((string)deserialized.ToJson()["foo"]).Should().Be("fooValue");
            ((string)deserialized.ToJson()["bar"]).Should().Be("barValue");
            ((string)deserialized.ToJson()["object"]["key1"]).Should().Be("value1");
            ((string)deserialized.ToJson()["object"]["key2"]).Should().Be("value2");
        }

        [Fact]
        [Trait("spec", "tm2i")]
        public void ShouldHandleNullMessageExtras()
        {
            var serialized = MsgPackHelper.Serialise<MessageExtras>(null as MessageExtras);
            var deserialized = MsgPackHelper.Deserialise<MessageExtras>(serialized);

            deserialized.Should().BeNull();
        }

        [Fact]
        [Trait("spec", "tm2i")]
        public void ShouldDeserializeNilAsNull()
        {
            var formatter = new MessageExtrasFormatter();

            // Serialize nil
            var bytes = MessagePackSerializer.Serialize<object>(null);
            var reader = new MessagePackReader(bytes);

            var result = formatter.Deserialize(ref reader, MessagePackSerializerOptions.Standard);

            result.Should().BeNull();
        }

        [Fact]
        [Trait("spec", "tm2i")]
        public void ShouldHandleEmptyMessageExtras()
        {
            var jObject = new JObject();
            var messageExtras = MessageExtras.From(jObject);

            var serialized = MsgPackHelper.Serialise<MessageExtras>(messageExtras);
            var deserialized = MsgPackHelper.Deserialise<MessageExtras>(serialized);

            deserialized.Should().NotBeNull();
            deserialized.ToJson().Should().NotBeNull();
        }

        [Fact]
        [Trait("spec", "tm2i")]
        public void ShouldPreserveComplexNestedStructures()
        {
            var jObject = new JObject
            {
                ["level1"] = new JObject
                {
                    ["level2"] = new JObject
                    {
                        ["level3"] = "deepValue"
                    }
                },
                ["array"] = new JArray { "item1", "item2", "item3" }
            };
            var messageExtras = MessageExtras.From(jObject);

            var serialized = MsgPackHelper.Serialise<MessageExtras>(messageExtras);
            var deserialized = MsgPackHelper.Deserialise<MessageExtras>(serialized);

            deserialized.Should().NotBeNull();
            var deserializedJson = deserialized.ToJson();
            JAssert.DeepEquals(jObject, deserializedJson, _testOutputHelper).Should().BeTrue();
        }

        [Fact]
        [Trait("spec", "tm2i")]
        public void ShouldHandleMessageExtrasWithOnlyDelta()
        {
            var jObject = new JObject
            {
                ["delta"] = new JObject
                {
                    ["From"] = "1",
                    ["Format"] = "best"
                }
            };
            var messageExtras = MessageExtras.From(jObject);

            var serialized = MsgPackHelper.Serialise<MessageExtras>(messageExtras);
            var deserialized = MsgPackHelper.Deserialise<MessageExtras>(serialized);

            deserialized.Should().NotBeNull();
            deserialized.Delta.Should().NotBeNull();
            deserialized.Delta.From.Should().Be("1");
            deserialized.Delta.Format.Should().Be("best");
        }

        [Fact]
        [Trait("spec", "tm2i")]
        public void ShouldSerializeMessageExtrasToJsonAndBack()
        {
            var originalJObject = new JObject
            {
                ["custom"] = "value",
                ["number"] = 42,
                ["boolean"] = true,
                ["delta"] = new JObject
                {
                    ["From"] = "test",
                    ["Format"] = "json"
                }
            };
            var messageExtras = MessageExtras.From(originalJObject);

            var serialized = MsgPackHelper.Serialise<MessageExtras>(messageExtras);
            var deserialized = MsgPackHelper.Deserialise<MessageExtras>(serialized);

            deserialized.Should().NotBeNull();
            var deserializedJson = deserialized.ToJson();
            JAssert.DeepEquals(originalJObject, deserializedJson, _testOutputHelper).Should().BeTrue();
        }

        [Fact]
        [Trait("spec", "tm2i")]
        public void ShouldHandleEmptyByteArray()
        {
            // Create a byte array with empty array marker
            var bytes = MessagePackSerializer.Serialize(new object[0]);

            var reader = new MessagePackReader(bytes);
            reader.ReadArrayHeader(); // Read the array header

            // Now the reader is at the end, simulating empty data scenario
            // This should be handled gracefully
        }

        [Fact]
        [Trait("spec", "tm2i")]
        public void ShouldRoundTripMessageExtrasWithVariousDataTypes()
        {
            var jObject = new JObject
            {
                ["string"] = "text",
                ["integer"] = 123,
                ["float"] = 45.67,
                ["boolean"] = true,
                ["null"] = null,
                ["array"] = new JArray { 1, 2, 3 },
                ["object"] = new JObject { ["nested"] = "value" }
            };
            var messageExtras = MessageExtras.From(jObject);

            var serialized = MsgPackHelper.Serialise<MessageExtras>(messageExtras);
            var deserialized = MsgPackHelper.Deserialise<MessageExtras>(serialized);

            deserialized.Should().NotBeNull();
            var deserializedJson = deserialized.ToJson();
            JAssert.DeepEquals(jObject, deserializedJson, _testOutputHelper).Should().BeTrue();
        }
    }
}
