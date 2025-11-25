using FluentAssertions;
using IO.Ably.MsgPack.CustomSerialisers;
using MessagePack;
using Xunit;

namespace IO.Ably.Tests.MsgPack.CustomSerializers
{
    public class CapabilityFormatterTests
    {
        [Fact]
        public void ShouldSerializeAndDeserializeAllowAllCapability()
        {
            var allAllowed = Capability.AllowAll;
            var serialized = MsgPackHelper.Serialise<Capability>(allAllowed);
            var deserialized = MsgPackHelper.Deserialise<Capability>(serialized);

            deserialized.Should().NotBeNull();
            deserialized.Should().Be(allAllowed);
            deserialized.ToJson().Should().Be(allAllowed.ToJson());
        }

        [Fact]
        public void ShouldSerializeAndDeserializeCapabilityWithOneResource()
        {
            var withOneResource = new Capability();
            withOneResource.AddResource("test").AllowPresence().AllowPublish().AllowSubscribe();

            var serialized = MsgPackHelper.Serialise<Capability>(withOneResource);
            var deserialized = MsgPackHelper.Deserialise<Capability>(serialized);

            deserialized.Should().NotBeNull();
            deserialized.Should().Be(withOneResource);
            deserialized.ToJson().Should().Be(withOneResource.ToJson());
        }

        [Fact]
        public void ShouldSerializeAndDeserializeCapabilityWithTwoResources()
        {
            var withTwoResources = new Capability();
            withTwoResources.AddResource("one").AllowAll();
            withTwoResources.AddResource("two").AllowPublish().AllowSubscribe();

            var serialized = MsgPackHelper.Serialise<Capability>(withTwoResources);
            var deserialized = MsgPackHelper.Deserialise<Capability>(serialized);

            deserialized.Should().NotBeNull();
            deserialized.Should().Be(withTwoResources);
            deserialized.ToJson().Should().Be(withTwoResources.ToJson());
        }

        [Fact]
        public void ShouldSerializeAndDeserializeEmptyCapability()
        {
            var emptyCapability = new Capability();

            var serialized = MsgPackHelper.Serialise<Capability>(emptyCapability);
            var deserialized = MsgPackHelper.Deserialise<Capability>(serialized);

            deserialized.Should().NotBeNull();
            deserialized.ToJson().Should().Be(emptyCapability.ToJson());
        }

        [Fact]
        public void ShouldHandleNullCapability()
        {
            var formatter = new CapabilityFormatter();
            var bufferWriter = new SimpleBufferWriter();
            var writer = new MessagePackWriter(bufferWriter);

            formatter.Serialize(ref writer, null, MessagePackSerializerOptions.Standard);
            writer.Flush();
            var bytes = bufferWriter.WrittenMemory.ToArray();

            var reader = new MessagePackReader(bytes);
            var result = formatter.Deserialize(ref reader, MessagePackSerializerOptions.Standard);

            result.Should().NotBeNull();
            result.ToJson().Should().BeEmpty();
        }

        [Fact]
        public void ShouldDeserializeNilAsEmptyCapability()
        {
            var formatter = new CapabilityFormatter();

            // Serialize nil
            var bytes = MessagePackSerializer.Serialize<object>(null);
            var reader = new MessagePackReader(bytes);

            var result = formatter.Deserialize(ref reader, MessagePackSerializerOptions.Standard);

            result.Should().NotBeNull();
            result.ToJson().Should().BeEmpty();
        }

        [Fact]
        public void ShouldSerializeCapabilityAsJsonString()
        {
            var capability = new Capability();
            capability.AddResource("channel1").AllowPublish();

            var formatter = new CapabilityFormatter();
            var bufferWriter = new SimpleBufferWriter();
            var writer = new MessagePackWriter(bufferWriter);

            formatter.Serialize(ref writer, capability, MessagePackSerializerOptions.Standard);
            writer.Flush();
            var bytes = bufferWriter.WrittenMemory.ToArray();

            // Deserialize as string to verify it's stored as JSON string
            var jsonString = MessagePackSerializer.Deserialize<string>(bytes);
            jsonString.Should().Be(capability.ToJson());
        }

        [Fact]
        public void ShouldDeserializeFromJsonString()
        {
            var expectedCapability = new Capability();
            expectedCapability.AddResource("test").AllowAll();
            var jsonString = expectedCapability.ToJson();

            // Serialize the JSON string
            var bytes = MessagePackSerializer.Serialize(jsonString);

            var formatter = new CapabilityFormatter();
            var reader = new MessagePackReader(bytes);
            var result = formatter.Deserialize(ref reader, MessagePackSerializerOptions.Standard);

            result.Should().NotBeNull();
            result.ToJson().Should().Be(expectedCapability.ToJson());
        }

        [Fact]
        public void ShouldHandleComplexCapabilityWithMultipleOperations()
        {
            var capability = new Capability();
            capability.AddResource("channel:*").AllowAll();
            capability.AddResource("private:*").AllowPublish().AllowSubscribe();
            capability.AddResource("presence:*").AllowPresence();

            var serialized = MsgPackHelper.Serialise<Capability>(capability);
            var deserialized = MsgPackHelper.Deserialise<Capability>(serialized);

            deserialized.Should().NotBeNull();
            deserialized.Should().Be(capability);
            deserialized.ToJson().Should().Be(capability.ToJson());
        }
    }
}
