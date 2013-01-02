using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Runtime.Serialization;
using Xunit.Extensions;

namespace Ably.Tests
{
    public class ApiKeyTests
    {

        [Theory]
        [InlineData("InvalidKey")]
        [InlineData("")]
        [InlineData(null)]
        public void Parse_WithKeyNotContaining3PartsSeparatedByColon_ThrowsInvalidKeyException(string key)
        {
            Assert.Throws<AblyInvalidApiKeyException>(delegate { ApiKey.Parse(key); });
        }

        [Fact]
        public void Parse_WithValidKeyReturns_ApiKeyWithAppIdKeyAndValue()
        {
            var key = ApiKey.Parse("AHSz6w:uQXPNQ:FGBZbsKSwqbCpkob");

            Assert.Equal(key.AppId, "AHSz6w");
            Assert.Equal(key.KeyId, "uQXPNQ");
            Assert.Equal(key.KeyValue, "FGBZbsKSwqbCpkob");
        }

        [Fact]
        public void Parse_WithValidKeyWithWhiteSpaceOnBothSides_ReturnsValidApiKeyObject()
        {
            var key = ApiKey.Parse(" AHSz6w:uQXPNQ:FGBZbsKSwqbCpkob ");

            Assert.Equal(key.AppId, "AHSz6w");
            Assert.Equal(key.KeyId, "uQXPNQ");
            Assert.Equal(key.KeyValue, "FGBZbsKSwqbCpkob");
        }
    }
}
