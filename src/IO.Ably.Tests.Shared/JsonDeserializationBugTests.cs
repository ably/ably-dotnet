using System;
using FluentAssertions;
using IO.Ably;
using Xunit;

namespace Ably.Tests
{
    public class JsonDeserializationBugTests
    {
        [Fact(Skip = "Needs some more investigation before I can fix this.")]
        public void ShouldPreserveTimezoneInforamtion()
        {
            var data = new TestClass(new DateTimeOffset(2014, 1, 1, 0,0,0, TimeSpan.Zero));
            var convertedData = JsonHelper.Serialize(data);
            var deserialisedData = JsonHelper.Deserialize<TestClass>(convertedData);
            data.Data.Should().Be(deserialisedData.Data);
        }
    }

    public class TestClass
    {
        public TestClass(object data)
        {
            Data = data;
        }

        public object Data { get; set; }
    }
}
