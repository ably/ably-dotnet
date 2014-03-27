using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ably.IntegrationTests.Auth
{
    [TestFixture]
    public class RequestTokenTests
    {
        [Test]
        public void RequestToken_WithNullParams_ReturnsTokenIssuedAroundNowAndExpiresInAnHour()
        {
            Ably.AblyOptions options = GetDefaultOptions();
            var ably = new Rest(options);

            Ably.Auth.Token token = ably.Auth.RequestToken(null, null);

            Assert.NotNull(token);
            Assert.True(((TimeSpan)(token.IssuedAt - DateTime.Now)).Minutes < 2);
            Assert.AreEqual(token.IssuedAt.AddHours(1), token.Expires); 
        }

        [Test]
        public void RequestToken_WithEmptyParameters_ReturnsTokenIssuedAroundNow()
        {
            var token = new Rest(GetDefaultOptions()).Auth.RequestToken(new TokenRequest(), new AuthOptions());

            Assert.NotNull(token);
            Assert.True(((TimeSpan)(token.IssuedAt - DateTime.Now)).Minutes < 2);
        }

        [Test]
        public void RequestToken_WithSpecificTtl_ReturnsTokenWithCorrectExpires()
        {
            Ably.AblyOptions options = GetDefaultOptions();
            var ably = new Rest(options);

            TokenRequest request = new TokenRequest { Ttl = TimeSpan.FromHours(8) };
            Ably.Auth.Token token = ably.Auth.RequestToken(request, null);

            Assert.NotNull(token);
            Assert.True(((TimeSpan)(token.IssuedAt - DateTime.Now)).Minutes < 2);
            Assert.AreEqual(token.IssuedAt.AddHours(8), token.Expires);
        }

       
        private static Ably.AblyOptions GetDefaultOptions()
        {
            var testData = TestsSetup.TestData;

            var options = new AblyOptions
            {
                Key = testData.keys[0].keyStr,
                Tls = testData.encrypted
            };
            return options;
        }
    }
}