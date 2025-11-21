using System;
using FluentAssertions;
using IO.Ably.MsgPack.CustomSerialisers;
using MessagePack;
using Xunit;

namespace IO.Ably.Tests.MsgPack.CustomSerializers
{
    public class TimespanFormatterTests
    {
        [MessagePackObject(keyAsPropertyName: true)]
        public class TestClass
        {
            public TestClass()
            {
            }

            public TestClass(TimeSpan data)
            {
                TimeSpan = data;
            }

            public TimeSpan TimeSpan { get; set; }
        }

        [Fact]
        public void ShouldSerializeTimeSpanToMilliseconds()
        {
            var originalTimeSpan = new TestClass(TimeSpan.FromSeconds(60));
            var serialized = MsgPackHelper.Serialise(originalTimeSpan);

            serialized.Should().NotBeNull();

            var deserialized = MsgPackHelper.Deserialise<TestClass>(serialized);
            deserialized.TimeSpan.Should().Be(originalTimeSpan.TimeSpan);
        }

        [Fact]
        public void ShouldPreserveTimeSpanValue()
        {
            var originalTimeSpan = new TestClass(TimeSpan.FromMinutes(5));
            var serialized = MsgPackHelper.Serialise(originalTimeSpan);
            var deserialized = MsgPackHelper.Deserialise<TestClass>(serialized);
            deserialized.TimeSpan.Should().Be(originalTimeSpan.TimeSpan);
        }

        [Fact]
        public void ShouldHandleIntegerTypeDeserialization()
        {
            var formatter = new TimespanFormatter();
            var expectedTimeSpan = TimeSpan.FromSeconds(60);
            var milliseconds = (long)expectedTimeSpan.TotalMilliseconds;

            // Serialize the milliseconds as integer
            var bytes = MessagePackSerializer.Serialize(milliseconds);
            var reader = new MessagePackReader(bytes);

            var result = formatter.Deserialize(ref reader, MessagePackSerializerOptions.Standard);
            result.Should().Be(expectedTimeSpan);
        }

        [Fact]
        public void ShouldHandleFloatTypeDeserialization()
        {
            var formatter = new TimespanFormatter();
            var expectedTimeSpan = TimeSpan.FromSeconds(60);
            var milliseconds = expectedTimeSpan.TotalMilliseconds;

            // Serialize the milliseconds as double
            var bytes = MessagePackSerializer.Serialize(milliseconds);
            var reader = new MessagePackReader(bytes);

            var result = formatter.Deserialize(ref reader, MessagePackSerializerOptions.Standard);
            result.Should().Be(expectedTimeSpan);
        }

        [Fact]
        public void ShouldHandleStringTypeDeserialization()
        {
            var formatter = new TimespanFormatter();
            var expectedTimeSpan = TimeSpan.FromSeconds(60);
            var timeSpanString = expectedTimeSpan.ToString();

            // Serialize the timespan as string
            var bytes = MessagePackSerializer.Serialize(timeSpanString);
            var reader = new MessagePackReader(bytes);

            var result = formatter.Deserialize(ref reader, MessagePackSerializerOptions.Standard);
            result.Should().Be(expectedTimeSpan);
        }

        [Fact]
        public void ShouldReturnMinValueForInvalidString()
        {
            var formatter = new TimespanFormatter();
            var invalidString = "invalid-timespan";

            var bytes = MessagePackSerializer.Serialize(invalidString);
            var reader = new MessagePackReader(bytes);

            var result = formatter.Deserialize(ref reader, MessagePackSerializerOptions.Standard);
            result.Should().Be(TimeSpan.MinValue);
        }

        [Fact]
        public void ShouldHandleZeroTimeSpan()
        {
            var originalTimeSpan = new TestClass(TimeSpan.Zero);
            var serialized = MsgPackHelper.Serialise(originalTimeSpan);
            var deserialized = MsgPackHelper.Deserialise<TestClass>(serialized);
            deserialized.TimeSpan.Should().Be(TimeSpan.Zero);
        }

        [Fact]
        public void ShouldHandleNegativeTimeSpan()
        {
            var originalTimeSpan = new TestClass(TimeSpan.FromSeconds(-30));
            var serialized = MsgPackHelper.Serialise(originalTimeSpan);
            var deserialized = MsgPackHelper.Deserialise<TestClass>(serialized);
            deserialized.TimeSpan.Should().Be(TimeSpan.FromSeconds(-30));
        }

        [MessagePackObject(keyAsPropertyName: true)]
        public class NullableTestClass
        {
            public NullableTestClass()
            {
            }

            public NullableTestClass(TimeSpan? data = null)
            {
                TimeSpan = data;
            }

            public TimeSpan? TimeSpan { get; set; }
        }

        [Fact]
        public void ShouldHandleNullableTimeSpan()
        {
            var originalTimeSpan = new NullableTestClass(TimeSpan.FromSeconds(60));
            var serialized = MsgPackHelper.Serialise(originalTimeSpan);
            var deserialized = MsgPackHelper.Deserialise<NullableTestClass>(serialized);
            deserialized.TimeSpan.Should().Be(originalTimeSpan.TimeSpan);
        }

        [Fact]
        public void ShouldHandleNullTimeSpan()
        {
            var originalTimeSpan = new NullableTestClass();
            var serialized = MsgPackHelper.Serialise(originalTimeSpan);
            var deserialized = MsgPackHelper.Deserialise<NullableTestClass>(serialized);
            deserialized.TimeSpan.Should().BeNull();
        }
    }
}
