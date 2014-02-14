using System.Net;
using Xunit;

namespace Ably.Tests
{
    public class ErrorInfoTests
    {
        [Fact]
        public void Parse_WithJsonResponseWhereJsonIsWrong_ReturnsUnknown500Error()
        {
            //Arrange
            var response = new AblyResponse() {TextResponse = "", Type = ResponseType.Json, StatusCode = (HttpStatusCode)500};

            //Act
            var errorInfo = ErrorInfo.Parse(response);

            //Assert
            Assert.Equal("Unknown error", errorInfo.Reason);
            Assert.Equal(500, errorInfo.Code);
            Assert.Equal(response.StatusCode, errorInfo.StatusCode);
        }

        [Fact]
        public void Parse_WithValidJsonResponse_RetrievesCodeAndReasonFromJson()
        {
            //Arrange
            var reason = "test";
            var code = 40400;
            var response = new AblyResponse() { TextResponse = string.Format("{{ \"error\": {{ \"code\":{0}, \"reason\":\"{1}\" }} }}",code, reason), Type = ResponseType.Json, StatusCode = (HttpStatusCode)500 };

            //Act
            var errorInfo = ErrorInfo.Parse(response);

            //Assert
            Assert.Equal(reason, errorInfo.Reason);
            Assert.Equal(code, errorInfo.Code);
        }

        [Fact]
        public void ToString_WithStatusCodeCodeAndReason_ReturnsFormattedString()
        {
            //Arrange
            var errorInfo = new ErrorInfo("Reason", 1000, HttpStatusCode.Accepted);

            //Assert
            Assert.Equal("Reason: Reason; Code: 1000; HttpStatusCode: (202)Accepted", errorInfo.ToString());
        }

        [Fact]
        public void ToString_WithCodeAndReasonWithoutStatusCode_ReturnsFormattedString()
        {
            //Arrange
            var errorInfo = new ErrorInfo("Reason", 1000);

            //Assert
            Assert.Equal("Reason: Reason; Code: 1000", errorInfo.ToString());
        }
    }
}