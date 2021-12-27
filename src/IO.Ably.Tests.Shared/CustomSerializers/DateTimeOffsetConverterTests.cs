using System;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace IO.Ably.Tests.Shared.CustomSerializers
{
    public class DateTimeOffsetConverterTests
    {

        internal class TestClass
        {
            public TestClass(DateTimeOffset data)
            {
                DateTimeOffset = data;
            }

            public DateTimeOffset DateTimeOffset { get; set; }
        }

        public class NullableTestClass
        {
            public NullableTestClass(DateTimeOffset? data = null)
            {
                DateTimeOffset = data;
            }

            public DateTimeOffset? DateTimeOffset { get; set; }
        }

        [Fact]
        public void ShouldConvertDateTimeOffsetToMilliseconds()
        {
            var originalDateTimeOffset = new TestClass(new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero));
            var serializedDateTimeOffset = JsonHelper.Serialize(originalDateTimeOffset);
            var serializedJToken = JToken.Parse(serializedDateTimeOffset);
            serializedJToken["DateTimeOffset"].Should().NotBeNull();
            serializedJToken["DateTimeOffset"].Type.Should().Be(JTokenType.Integer);
            serializedJToken["DateTimeOffset"].Value<long>().Should().Be(1388534400000);
        }

        [Fact]
        public void ShouldPreserveTimezoneInformation()
        {
            var originalDateTimeOffset = new TestClass(new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero));

            var serializedDateTimeOffset = JsonHelper.Serialize(originalDateTimeOffset);
            var deserializedDateTimeOffset = JsonHelper.Deserialize<TestClass>(serializedDateTimeOffset);

            deserializedDateTimeOffset.DateTimeOffset.Should().Be(originalDateTimeOffset.DateTimeOffset);
        }

        [Fact]
        public void ShouldSetMinDateTimeOffsetForExcludedProperty()
        {

        }

        [Fact]
        public void ShouldExcludeNullDateTimeOffset()
        {

        }

        [Fact]
        public void ShouldSetNullForExcludedProperty()
        {

        }
    }
}
