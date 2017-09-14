using Xunit;

namespace IO.Ably.Tests
{
    public class ApiKeyTests
    {

        [Theory]
        [InlineData("InvalidKey")]
        [InlineData("")]
        [InlineData(null)]
        public void Parse_WithKeyNotContaining3PartsSeparatedByColon_ThrowsInvalidKeyException(string key)
        {
            Assert.Throws<AblyException>(delegate { ApiKey.Parse(key); });
        }

        [Fact]
        public void Parse_WithValidKeyReturns_ApiKeyWithAppIdKeyAndValue()
        {
            var key = ApiKey.Parse("123.456:789");

            Assert.Equal("123", key.AppId);
            Assert.Equal("123.456", key.KeyName);
            Assert.Equal("789", key.KeySecret);
        }

        [Fact]
        public void Parse_WithValidKeyWithWhiteSpaceOnBothSides_ReturnsValidApiKeyObject()
        {
            var key = ApiKey.Parse(" 123.456:789 ");

            Assert.Equal("123", key.AppId);
            Assert.Equal("123.456", key.KeyName);
            Assert.Equal("789", key.KeySecret);
        }
    }
}
