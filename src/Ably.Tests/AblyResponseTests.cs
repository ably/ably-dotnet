using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Extensions;

namespace IO.Ably.Tests
{
    public class AblyResponseTests
    {
        [Theory]
        [InlineData("application/json", ResponseType.Json)]
        [InlineData("application/x-msgpack", ResponseType.Binary)]
        [InlineData("", ResponseType.Binary)]
        public void Ctor_WithContentType_SetsTypeCorrectly(string type, object responseType)
        {
            //Arrange
                        

            //Act
            var response = new AblyResponse("", type, new byte[0]);

            //Assert
            Assert.Equal((ResponseType)responseType, response.Type);
        }

        [Theory]
        [InlineData("utf-7", "utf-7")]
        [InlineData("", "utf-8")]
        public void Ctor_WithEncoding_SetsEncodingCorrectly(string encoding, string expected)
        {
            //Arrange


            //Act
            var response = new AblyResponse(encoding, "", new byte[0]);

            //Assert
            Assert.Equal(expected, response.Encoding);
        }


        [Fact]
        public void Ctor_WhenTypeIsJson_SetsTextResponse()
        {
            //Arrange
            var text = "Test";

            //Act
            var response = new AblyResponse("", "application/json", text.GetBytes());

            //Assert
            Assert.Equal(text, response.TextResponse);
        }

    }
}
