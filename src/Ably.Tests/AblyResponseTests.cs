using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Extensions;

namespace Ably.Tests
{
    public class AblyResponseTests
    {
        [Theory]
        [InlineData("application/jSon", ResponseType.Json)]
        [InlineData("application/binary", ResponseType.Thrift)]
        [InlineData("", ResponseType.Thrift)]
        public void Ctor_WithContentType_SetsTypeCorrectly(string type, ResponseType responseType)
        {
            //Arrange
                        

            //Act
            var response = new AblyResponse("", type, new byte[0]);

            //Assert
            Assert.Equal(responseType, response.Type);
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
