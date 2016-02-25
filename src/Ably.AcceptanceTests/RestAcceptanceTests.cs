using System;
using System.Net;
using FluentAssertions;
using NUnit.Framework;

namespace IO.Ably.AcceptanceTests
{
    public class ChannelAcceptanceTests
    {

    }

    [TestFixture]
    public class RestAcceptanceTests
    {
        [Test]
        public void ShouldReturnTheRequestedToken()
        {
            //Arrange
            var fakeKey = "AppId.KeyId:KeyValue";
            var ably = new RestClient(new AblyOptions() { Key = fakeKey, Environment = AblyEnvironment.Sandbox });

            //Act
            var error = Assert.Throws<AblyException>(delegate { ably.Channels.Get("Test").Publish("test", true); });

            error.ErrorInfo.statusCode.Should().Be(HttpStatusCode.Unauthorized);
            error.ErrorInfo.code.Should().Be(40100);
        }
    }
}
