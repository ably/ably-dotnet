using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Xunit;
using System.Threading;
using Xunit.Extensions;
using Newtonsoft.Json.Linq;

namespace Ably.Tests
{
    public class MessageTests
    {
        [Fact]
        public void IsBinaryMessage_WithByteArrayData_ReturnsTrue()
        {
            var message = new Message { Data = new byte[] { 1, 2} };
            Assert.True(message.IsBinaryMessage);
        }

        
        [Fact]
        public void Value_WithBinarryMessageAndTypeIsNOTByteArray_ThrowsInvalidOperationException()
        {
            var message = new Message { Data = new byte[] { 1,2} };
            Assert.Throws<InvalidOperationException>(delegate { message.Value<int>(); });
        }

        [Fact]
        public void Value_WithIntJToken_ReturnsCorrectValue()
        {
            var message = new Message { Data = JToken.Parse("4") };
            Assert.Equal(4, message.Value<int>());
        }

        public class TestClass
        {
            public string Name { get; set; }
            public int Number { get; set; }
            public decimal Money { get; set; }
        }

        [Fact]
        public void Value_WithJTokenHoldingObject_ReturnsCorrectValue()
        {
            var json = "{ \"Name\": \"Martin\", \"Number\":10, \"Money\": 200.40 }";
            var message = new Message { Data = JToken.Parse(json)};
            var test = message.Value<TestClass>();
            Assert.NotNull(test);
            Assert.Equal("Martin", test.Name);
            Assert.Equal(10, test.Number);
            Assert.Equal(200.40m, test.Money);
        }


    }
}
