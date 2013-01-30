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
            var testData = TestsSetup.TestData;

            var options = new AblyOptions {
                Key = "TiHk3g:3lJG9Q:R8KadsOydTRCNMOp", 
                Host = "rest.ably.io",
                Encrypted = testData.encrypted
            };
            var ably = new Rest(options);

            Ably.Auth.Token token = ably.Auth.RequestToken(null, null);

            Assert.NotNull(token);
            Assert.True(((TimeSpan)(token.IssuedAt - DateTime.Now)).Minutes < 2);
            Assert.AreEqual(token.IssuedAt.AddHours(1), token.Expires); 
        }
    }
}