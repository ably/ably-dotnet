using System.Buffers;
using FluentAssertions;
using IO.Ably.MsgPack.CustomSerialisers;
using IO.Ably.Tests.Shared.Helpers;
using IO.Ably.Tests.Shared.MsgPack;
using MessagePack;
using MessagePack.Formatters;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.MsgPack.CustomSerializers
{
    public class JObjectMessagePackSerializerTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        private MessagePackSerializerOptions _msgPackTestOptions = MsgPackTestExtensions.GetTestOptions();

        public JObjectMessagePackSerializerTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void ShouldSerializeAndDeserializeSimpleJObject()
        {
            var jObject = new JObject
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            };

            var serialized = MsgPackHelper.Serialise<JObject>(jObject);
            var deserialized = MsgPackHelper.Deserialise<JObject>(serialized);

            deserialized.Should().NotBeNull();
            JAssert.DeepEquals(jObject, deserialized, _testOutputHelper).Should().BeTrue();
        }

        [Fact]
        public void ShouldSerializeAndDeserializeNestedJObject()
        {
            var jObject = new JObject
            {
                ["level1"] = new JObject
                {
                    ["level2"] = new JObject
                    {
                        ["level3"] = "deepValue"
                    }
                }
            };

            var serialized = MsgPackHelper.Serialise<JObject>(jObject);
            var deserialized = MsgPackHelper.Deserialise<JObject>(serialized);

            deserialized.Should().NotBeNull();
            JAssert.DeepEquals(jObject, deserialized, _testOutputHelper).Should().BeTrue();
        }

        [Fact]
        public void ShouldSerializeAndDeserializeJObjectWithArray()
        {
            var jObject = new JObject
            {
                ["array"] = new JArray { "item1", "item2", "item3" },
                ["number"] = 42
            };

            var serialized = MsgPackHelper.Serialise<JObject>(jObject);
            var deserialized = MsgPackHelper.Deserialise<JObject>(serialized);

            deserialized.Should().NotBeNull();
            JAssert.DeepEquals(jObject, deserialized, _testOutputHelper).Should().BeTrue();
        }

        [Fact]
        public void ShouldSerializeAndDeserializeJObjectWithVariousDataTypes()
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

            var serialized = MsgPackHelper.Serialise<JObject>(jObject);
            var deserialized = MsgPackHelper.Deserialise<JObject>(serialized);

            deserialized.Should().NotBeNull();
            JAssert.DeepEquals(jObject, deserialized, _testOutputHelper).Should().BeTrue();
        }

        [Fact]
        public void ShouldHandleNullJObject()
        {
            var formatter = new JObjectMessagePackSerializer();
            var bufferWriter = new SimpleBufferWriter();
            var writer = new MessagePackWriter(bufferWriter);

            ((IMessagePackFormatter<JObject>)formatter).Serialize(ref writer, null, MessagePackSerializerOptions.Standard);
            writer.Flush();
            var bytes = bufferWriter.WrittenSpan.ToArray();

            var reader = new MessagePackReader(bytes);
            var result = ((IMessagePackFormatter<JObject>)formatter).Deserialize(ref reader, MessagePackSerializerOptions.Standard);

            result.Should().BeNull();
        }

        [Fact]
        public void ShouldDeserializeNilAsNull()
        {
            var formatter = new JObjectMessagePackSerializer();

            // Serialize nil
            var bytes = MessagePackSerializer.Serialize<object>(null);
            var reader = new MessagePackReader(bytes);

            var result = ((IMessagePackFormatter<JObject>)formatter).Deserialize(ref reader, MessagePackSerializerOptions.Standard);

            result.Should().BeNull();
        }

        [Fact]
        public void ShouldHandleEmptyJObject()
        {
            var jObject = new JObject();

            var serialized = MsgPackHelper.Serialise<JObject>(jObject);
            var deserialized = MsgPackHelper.Deserialise<JObject>(serialized);

            deserialized.Should().NotBeNull();
            deserialized.Count.Should().Be(0);
        }

        [Fact]
        public void ShouldPreservePropertyOrder()
        {
            var jObject = new JObject
            {
                ["first"] = 1,
                ["second"] = 2,
                ["third"] = 3
            };

            var serialized = MsgPackHelper.Serialise<JObject>(jObject);
            var deserialized = MsgPackHelper.Deserialise<JObject>(serialized);

            deserialized.Should().NotBeNull();
            JAssert.DeepEquals(jObject, deserialized, _testOutputHelper).Should().BeTrue();
        }

        [Fact]
        public void ShouldHandleComplexNestedStructures()
        {
            var jObject = new JObject
            {
                ["users"] = new JArray
                {
                    new JObject
                    {
                        ["name"] = "Alice",
                        ["age"] = 30,
                        ["active"] = true
                    },
                    new JObject
                    {
                        ["name"] = "Bob",
                        ["age"] = 25,
                        ["active"] = false
                    }
                },
                ["metadata"] = new JObject
                {
                    ["version"] = "1.0",
                    ["timestamp"] = 1234567890
                }
            };

            var serialized = MsgPackHelper.Serialise<JObject>(jObject);
            var deserialized = MsgPackHelper.Deserialise<JObject>(serialized);

            deserialized.Should().NotBeNull();
            JAssert.DeepEquals(jObject, deserialized, _testOutputHelper).Should().BeTrue();
        }

        [Fact]
        public void ShouldHandleSpecialCharacters()
        {
            var jObject = new JObject
            {
                ["unicode"] = "こんにちは",
                ["emoji"] = "😀🎉",
                ["special"] = "!@#$%^&*()",
                ["quotes"] = "\"quoted\"",
                ["newline"] = "line1\nline2"
            };

            var serialized = MsgPackHelper.Serialise<JObject>(jObject);
            var deserialized = MsgPackHelper.Deserialise<JObject>(serialized);

            deserialized.Should().NotBeNull();
            JAssert.DeepEquals(jObject, deserialized, _testOutputHelper).Should().BeTrue();
        }

        [Fact]
        public void ShouldHandleLargeNumbers()
        {
            var jObject = new JObject
            {
                ["int32Max"] = int.MaxValue,
                ["int64Max"] = long.MaxValue,
                ["double"] = double.MaxValue,
                ["negative"] = -999999999
            };

            var serialized = MsgPackHelper.Serialise<JObject>(jObject);
            var deserialized = MsgPackHelper.Deserialise<JObject>(serialized);

            deserialized.Should().NotBeNull();
            deserialized["int32Max"].Value<int>().Should().Be(int.MaxValue);
            deserialized["int64Max"].Value<long>().Should().Be(long.MaxValue);
            deserialized["negative"].Value<int>().Should().Be(-999999999);
        }

        [Fact]
        public void ShouldHandleEmptyArrays()
        {
            var jObject = new JObject
            {
                ["emptyArray"] = new JArray(),
                ["nonEmptyArray"] = new JArray { 1, 2, 3 }
            };

            var serialized = MsgPackHelper.Serialise<JObject>(jObject);
            var deserialized = MsgPackHelper.Deserialise<JObject>(serialized);

            deserialized.Should().NotBeNull();
            JAssert.DeepEquals(jObject, deserialized, _testOutputHelper).Should().BeTrue();
        }

        [Fact]
        public void ShouldHandleNullValues()
        {
            var jObject = new JObject
            {
                ["nullValue"] = null,
                ["notNull"] = "value"
            };

            var serialized = MsgPackHelper.Serialise<JObject>(jObject);
            var deserialized = MsgPackHelper.Deserialise<JObject>(serialized);

            deserialized.Should().NotBeNull();
            deserialized["nullValue"].Type.Should().Be(JTokenType.Null);
            deserialized["notNull"].Value<string>().Should().Be("value");
        }

        [Fact]
        public void ShouldConvertToAndFromJson()
        {
            var originalJson = @"{
                ""name"": ""test"",
                ""value"": 123,
                ""nested"": {
                    ""key"": ""value""
                }
            }";
            var jObject = JObject.Parse(originalJson);

            var serialized = MsgPackHelper.Serialise<JObject>(jObject);
            var deserialized = MsgPackHelper.Deserialise<JObject>(serialized);

            deserialized.Should().NotBeNull();
            JAssert.DeepEquals(jObject, deserialized, _testOutputHelper).Should().BeTrue();
        }

        [Fact]
        public void ShouldHandleEmptyByteArrayDeserialization()
        {
            var formatter = new JObjectMessagePackSerializer();

            // Create empty raw bytes using a simple buffer writer (compatible with .NET Standard 2.0)
            var bufferWriter = new SimpleBufferWriter();
            var writer = new MessagePackWriter(bufferWriter);
            writer.WriteRaw(new byte[0]);
            writer.Flush();
            var bytes = bufferWriter.WrittenMemory.ToArray();

            var reader = new MessagePackReader(bytes);
            var result = ((IMessagePackFormatter<JObject>)formatter).Deserialize(ref reader, MessagePackSerializerOptions.Standard);

            result.Should().BeNull();
        }

        [Fact]
        public void ShouldDeserializeJObjectAsPartOfLargerStructure()
        {
            // Test that JObject can be deserialized when it's a value in a larger MessagePack structure
            // This ensures our empty buffer check doesn't interfere with reading from a larger stream

            var jObjectData = new JObject
            {
                ["key1"] = "value1",
                ["key2"] = 42,
                ["nested"] = new JObject
                {
                    ["inner"] = "innerValue"
                }
            };

            // Use a concrete class with MessagePackObject attribute
            var containerData = new TestContainer
            {
                Id = 123,
                Name = "test",
                Data = jObjectData,
                Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var serialized = MsgPackHelper.Serialise<TestContainer>(containerData, _msgPackTestOptions);
            var deserialized = MsgPackHelper.Deserialise<TestContainer>(serialized, _msgPackTestOptions);

            deserialized.Should().NotBeNull();
            deserialized.Id.Should().Be(123);
            deserialized.Name.Should().Be("test");
            deserialized.Data.Should().NotBeNull();

            // The data should be deserialized as a JObject
            deserialized.Data.Should().BeOfType<JObject>();
            JAssert.DeepEquals(jObjectData, deserialized.Data, _testOutputHelper).Should().BeTrue();
        }

        [MessagePackObject(keyAsPropertyName: true)]
        public class TestContainer
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public JObject Data { get; set; }

            public long Timestamp { get; set; }
        }
    }
}
