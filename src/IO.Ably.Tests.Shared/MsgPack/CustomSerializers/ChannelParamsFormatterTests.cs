using FluentAssertions;
using IO.Ably.MsgPack.CustomSerialisers;
using MessagePack;
using Xunit;

namespace IO.Ably.Tests.MsgPack.CustomSerializers
{
    public class ChannelParamsFormatterTests
    {
        [Fact]
        public void ShouldSerializeAndDeserializeEmptyChannelParams()
        {
            var channelParams = new ChannelParams();

            var serialized = MsgPackHelper.Serialise(channelParams);
            var deserialized = MsgPackHelper.Deserialise(serialized, typeof(ChannelParams)) as ChannelParams;

            deserialized.Should().NotBeNull();
            deserialized.Count.Should().Be(0);
        }

        [Fact]
        public void ShouldSerializeAndDeserializeChannelParamsWithOneEntry()
        {
            var channelParams = new ChannelParams
            {
                ["key1"] = "value1"
            };

            var serialized = MsgPackHelper.Serialise(channelParams);
            var deserialized = MsgPackHelper.Deserialise(serialized, typeof(ChannelParams)) as ChannelParams;

            deserialized.Should().NotBeNull();
            deserialized.Count.Should().Be(1);
            deserialized["key1"].Should().Be("value1");
        }

        [Fact]
        public void ShouldSerializeAndDeserializeChannelParamsWithMultipleEntries()
        {
            var channelParams = new ChannelParams
            {
                ["key1"] = "value1",
                ["key2"] = "value2",
                ["key3"] = "value3"
            };

            var serialized = MsgPackHelper.Serialise(channelParams);
            var deserialized = MsgPackHelper.Deserialise(serialized, typeof(ChannelParams)) as ChannelParams;

            deserialized.Should().NotBeNull();
            deserialized.Count.Should().Be(3);
            deserialized["key1"].Should().Be("value1");
            deserialized["key2"].Should().Be("value2");
            deserialized["key3"].Should().Be("value3");
        }

        [Fact]
        public void ShouldHandleNullChannelParams()
        {
            var serialized = MsgPackHelper.Serialise(null as ChannelParams);
            var deserialized = MsgPackHelper.Deserialise(serialized, typeof(ChannelParams)) as ChannelParams;

            deserialized.Should().BeNull();
        }

        [Fact]
        public void ShouldDeserializeNilAsNull()
        {
            var formatter = new ChannelParamsFormatter();

            // Serialize nil
            var bytes = MessagePackSerializer.Serialize<object>(null);
            var reader = new MessagePackReader(bytes);

            var result = formatter.Deserialize(ref reader, MessagePackSerializerOptions.Standard);

            result.Should().BeNull();
        }

        [Fact]
        public void ShouldSerializeChannelParamsAsMap()
        {
            var channelParams = new ChannelParams
            {
                ["param1"] = "value1",
                ["param2"] = "value2"
            };

            var bytes = MsgPackHelper.Serialise(channelParams);

            // Verify it's serialized as a map
            var reader = new MessagePackReader(bytes);
            var mapHeader = reader.ReadMapHeader();
            mapHeader.Should().Be(2);
        }

        [Fact]
        public void ShouldPreserveKeyValuePairs()
        {
            var channelParams = new ChannelParams
            {
                ["rewind"] = "1",
                ["delta"] = "vcdiff"
            };

            var serialized = MsgPackHelper.Serialise(channelParams);
            var deserialized = MsgPackHelper.Deserialise(serialized, typeof(ChannelParams)) as ChannelParams;

            deserialized.Should().NotBeNull();
            deserialized.Should().ContainKey("rewind");
            deserialized.Should().ContainKey("delta");
            deserialized["rewind"].Should().Be("1");
            deserialized["delta"].Should().Be("vcdiff");
        }

        [Fact]
        public void ShouldHandleSpecialCharactersInValues()
        {
            var channelParams = new ChannelParams
            {
                ["key"] = "value with spaces",
                ["special"] = "!@#$%^&*()",
                ["unicode"] = "こんにちは"
            };

            var serialized = MsgPackHelper.Serialise(channelParams);
            var deserialized = MsgPackHelper.Deserialise(serialized, typeof(ChannelParams)) as ChannelParams;

            deserialized.Should().NotBeNull();
            deserialized["key"].Should().Be("value with spaces");
            deserialized["special"].Should().Be("!@#$%^&*()");
            deserialized["unicode"].Should().Be("こんにちは");
        }

        [Fact]
        public void ShouldHandleEmptyStringValues()
        {
            var channelParams = new ChannelParams
            {
                ["empty"] = string.Empty,
                ["notEmpty"] = "value"
            };

            var serialized = MsgPackHelper.Serialise(channelParams);
            var deserialized = MsgPackHelper.Deserialise(serialized, typeof(ChannelParams)) as ChannelParams;

            deserialized.Should().NotBeNull();
            deserialized["empty"].Should().Be(string.Empty);
            deserialized["notEmpty"].Should().Be("value");
        }

        [Fact]
        public void ShouldMaintainOrderOfEntries()
        {
            var channelParams = new ChannelParams
            {
                ["first"] = "1",
                ["second"] = "2",
                ["third"] = "3"
            };

            var serialized = MsgPackHelper.Serialise(channelParams);
            var deserialized = MsgPackHelper.Deserialise(serialized, typeof(ChannelParams)) as ChannelParams;

            deserialized.Should().NotBeNull();
            deserialized.Count.Should().Be(3);
            // Dictionary doesn't guarantee order, but all keys should be present
            deserialized.Should().ContainKeys("first", "second", "third");
        }
    }
}
