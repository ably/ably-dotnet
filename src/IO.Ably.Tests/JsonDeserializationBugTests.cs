using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably;
using Newtonsoft.Json;
using Xunit;

namespace Ably.Tests
{
    public class JsonDeserializationBugTests
    {
        [Fact(Skip = "Needs some more investigation before I can fix this.")]
        public void ShouldPreserveTimezoneInforamtion()
        {
            var data = new TestClass(new DateTimeOffset(2014, 1, 1, 0,0,0, TimeSpan.Zero));
            var convertedData = JsonConvert.SerializeObject(data, Config.GetJsonSettings());
            var deserialisedData = JsonConvert.DeserializeObject<TestClass>(convertedData, Config.GetJsonSettings());
            data.Data.Should().Be(deserialisedData.Data);
        }
    }

    public class TestClass
    {
        public TestClass(object data)
        {
            this.Data = data;
        }

        public object Data { get; set; }
    }
}
