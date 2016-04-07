using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace IO.Ably.AcceptanceTests
{
    [TestFixture]
    public class RestAcceptanceTests
    {
        [Test]
        public async Task PublishingAMessageWithInvalidKey_ReturnsUnAuthorized()
        {
            //Arrange
            var fakeKey = $"{TestsSetup.TestData.appId}.KeyId:KeyValue";
            var ably = new AblyRest(new AblyOptions() { Key = fakeKey, Environment = AblyEnvironment.Sandbox , Tls = true});

            //Act
            var error = Assert.ThrowsAsync<AblyException>(() => ably.Channels.Get("Test").Publish("test", true));

            error.ErrorInfo.statusCode.Should().Be(HttpStatusCode.Unauthorized);
            error.ErrorInfo.code.Should().Be(40400);
        }
    }
}
