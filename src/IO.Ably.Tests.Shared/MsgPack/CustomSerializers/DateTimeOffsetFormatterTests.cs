using System;
using FluentAssertions;
using IO.Ably.MsgPack.CustomSerialisers;
using IO.Ably.Tests.Shared.MsgPack;
using MessagePack;
using Xunit;

namespace IO.Ably.Tests.MsgPack.CustomSerializers
{
    public class DateTimeOffsetFormatterTests
    {
        private MessagePackSerializerOptions _msgPackTestOptions = MsgPackTestExtensions.GetTestOptions();

        [MessagePackObject(keyAsPropertyName: true)]
        public class TestClass
        {
            public TestClass()
            {
            }

            public TestClass(DateTimeOffset data)
            {
                DateTimeOffset = data;
            }

            public DateTimeOffset DateTimeOffset { get; set; }
        }

        [Fact]
        public void ShouldSerializeDateTimeOffsetToMilliseconds()
        {
            var originalDateTimeOffset = new TestClass(new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero));
            var serialized = MsgPackHelper.Serialise<TestClass>(originalDateTimeOffset, _msgPackTestOptions);

            serialized.Should().NotBeNull();

            var deserialized = MsgPackHelper.Deserialise<TestClass>(serialized, _msgPackTestOptions);
            deserialized.DateTimeOffset.Should().Be(originalDateTimeOffset.DateTimeOffset);
        }

        [Fact]
        public void ShouldPreserveTimezoneInformation()
        {
            var originalDateTimeOffset = new TestClass(new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero));
            var serialized = MsgPackHelper.Serialise<TestClass>(originalDateTimeOffset, _msgPackTestOptions);
            var deserialized = MsgPackHelper.Deserialise<TestClass>(serialized, _msgPackTestOptions);
            deserialized.DateTimeOffset.Should().Be(originalDateTimeOffset.DateTimeOffset);
        }

        [Fact]
        public void ShouldHandleIntegerTypeDeserialization()
        {
            var formatter = new DateTimeOffsetFormatter();
            var expectedDate = new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var milliseconds = expectedDate.ToUnixTimeInMilliseconds();

            // Serialize the milliseconds as integer
            var bytes = MessagePackSerializer.Serialize(milliseconds);
            var reader = new MessagePackReader(bytes);

            var result = formatter.Deserialize(ref reader, MessagePackSerializerOptions.Standard);
            result.Should().Be(expectedDate);
        }

        [Fact]
        public void ShouldHandleFloatTypeDeserialization()
        {
            var formatter = new DateTimeOffsetFormatter();
            var expectedDate = new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var milliseconds = (double)expectedDate.ToUnixTimeInMilliseconds();

            // Serialize the milliseconds as double
            var bytes = MessagePackSerializer.Serialize(milliseconds);
            var reader = new MessagePackReader(bytes);

            var result = formatter.Deserialize(ref reader, MessagePackSerializerOptions.Standard);
            result.Should().Be(expectedDate);
        }

        [Fact]
        public void ShouldHandleStringTypeDeserialization()
        {
            var formatter = new DateTimeOffsetFormatter();
            var expectedDate = new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var dateString = expectedDate.ToString("O");

            // Serialize the date as string
            var bytes = MessagePackSerializer.Serialize(dateString);
            var reader = new MessagePackReader(bytes);

            var result = formatter.Deserialize(ref reader, MessagePackSerializerOptions.Standard);
            result.Should().Be(expectedDate);
        }

        [Fact]
        public void ShouldReturnMinValueForInvalidString()
        {
            var formatter = new DateTimeOffsetFormatter();
            var invalidString = "invalid-date";

            var bytes = MessagePackSerializer.Serialize(invalidString);
            var reader = new MessagePackReader(bytes);

            var result = formatter.Deserialize(ref reader, MessagePackSerializerOptions.Standard);
            result.Should().Be(DateTimeOffset.MinValue);
        }

        [MessagePackObject(keyAsPropertyName: true)]
        public class NullableTestClass
        {
            public NullableTestClass()
            {
            }

            public NullableTestClass(DateTimeOffset? data = null)
            {
                DateTimeOffset = data;
            }

            public DateTimeOffset? DateTimeOffset { get; set; }
        }

        [Fact]
        public void ShouldHandleNullableDateTimeOffset()
        {
            var originalDateTimeOffset = new NullableTestClass(new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero));
            var serialized = MsgPackHelper.Serialise<NullableTestClass>(originalDateTimeOffset, _msgPackTestOptions);
            var deserialized = MsgPackHelper.Deserialise<NullableTestClass>(serialized, _msgPackTestOptions);
            deserialized.DateTimeOffset.Should().Be(originalDateTimeOffset.DateTimeOffset);
        }

        [Fact]
        public void ShouldHandleNullDateTimeOffset()
        {
            var originalDateTimeOffset = new NullableTestClass();
            var serialized = MsgPackHelper.Serialise<NullableTestClass>(originalDateTimeOffset, _msgPackTestOptions);
            var deserialized = MsgPackHelper.Deserialise<NullableTestClass>(serialized, _msgPackTestOptions);
            deserialized.DateTimeOffset.Should().BeNull();
        }
    }
}
