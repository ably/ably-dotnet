using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;

namespace Ably.IntegrationTests
{
    [TestFixture]
    public class AblyExceptionTests
    {
        [Test]
        public void RequestToken_WithInvalidHost_ReturnErrorWith404Status()
        {
            var options = GetDefaultOptions();
            options.Host = "invalid.ably.io";
            var rest = new Rest(options);

            var ex = Assert.Throws<AblyException>(delegate
            {
                rest.Auth.RequestToken(null, null);
            });

            Assert.That(ex.InnerException, Is.InstanceOf<WebException>());
        }

        [Test]
        public void RequestToken_WithInvalidExpiryTime_ThrowsAblyException()
        {
            var options = GetDefaultOptions();
            var rest = new Rest(options);

            var request = new TokenRequest { Ttl = TimeSpan.FromDays(999) };
            var ex = Assert.Throws<AblyException>(delegate
            {
                rest.Auth.RequestToken(request, null);
            });

            Assert.AreEqual(HttpStatusCode.BadRequest, ex.ErrorInfo.StatusCode.Value);
            Assert.AreEqual("40003", ex.ErrorInfo.Code);
        }

        private static Ably.AblyOptions GetDefaultOptions()
        {
            var testData = TestsSetup.TestData;

            var options = new AblyOptions
            {
                Key = testData.keys[0].keyStr,
                Tls = testData.tls
            };
            return options;
        }
    }
}
