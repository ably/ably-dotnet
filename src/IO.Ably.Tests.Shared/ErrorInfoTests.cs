using System.Net;
using Xunit;

namespace IO.Ably.Tests
{
    public class ErrorInfoTests
    {
        [Fact]
        public void Parse_WithJsonResponseWhereJsonIsWrong_ReturnsUnknown500Error()
        {
            // Arrange
            var response = new AblyResponse() { TextResponse = string.Empty, Type = ResponseType.Json, StatusCode = (HttpStatusCode)500 };

            // Act
            var errorInfo = ErrorInfo.Parse(response);

            // Assert
            Assert.Equal("Unknown error", errorInfo.Message);
            Assert.Equal(50000, errorInfo.Code);
            Assert.Equal(response.StatusCode, errorInfo.StatusCode);
        }

        [Fact]
        public void Parse_WithValidJsonResponse_RetrievesCodeAndReasonFromJson()
        {
            // Arrange
            var reason = "test";
            var code = 40400;
            var response = new AblyResponse() { TextResponse = string.Format("{{ \"error\": {{ \"code\":{0}, \"message\":\"{1}\" }} }}", code, reason), Type = ResponseType.Json, StatusCode = (HttpStatusCode)500 };

            // Act
            var errorInfo = ErrorInfo.Parse(response);

            // Assert
            Assert.Equal(reason, errorInfo.Message);
            Assert.Equal(code, errorInfo.Code);
        }

        [Fact]
        [Trait("spec", "TI4")]
        [Trait("spec", "TI5")]
        public void ToString_WithStatusCodeCodeAndReason_ReturnsFormattedString_WithHrefFromCode()
        {
            // Arrange
            var errorInfo = new ErrorInfo("Error Reason", 1000, HttpStatusCode.Accepted);

            // Assert
            Assert.Equal("[ErrorInfo Reason: Error Reason (See https://help.ably.io/error/1000); Code: 1000; StatusCode: 202 (Accepted); Href: https://help.ably.io/error/1000;]", errorInfo.ToString());
        }

        [Fact]
        [Trait("spec", "TI4")]
        [Trait("spec", "TI5")]
        public void ToString_WithCodeAndReasonWithoutStatusCodeAndWithoutHref_ReturnsFormattedString_WithHrefFromCode()
        {
            // Arrange
            var errorInfo = new ErrorInfo("Reason", 1000);

            // Assert
            Assert.Equal("[ErrorInfo Reason: Reason (See https://help.ably.io/error/1000); Code: 1000; Href: https://help.ably.io/error/1000;]", errorInfo.ToString());
        }

        [Fact]
        [Trait("spec", "TI4")]
        [Trait("spec", "TI5")]
        public void ToString_WithCodeAndHref_ReturnsFormattedString_ThatUsesHref()
        {
            // Arrange
            var errorInfo = new ErrorInfo("Reason", 1000, null, "http://example.com");

            // Assert
            Assert.Equal("[ErrorInfo Reason: Reason (See http://example.com); Code: 1000; Href: http://example.com;]", errorInfo.ToString());
        }
    }
}
