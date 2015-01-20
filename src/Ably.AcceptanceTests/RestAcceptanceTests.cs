using System;
using System.Net;
using FluentAssertions;
using NUnit.Framework;

namespace Ably.AcceptanceTests
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
            var ably = new Rest(new AblyOptions() { Key = fakeKey, Environment = AblyEnvironment.Sandbox });

            //Act
            var error = Assert.Throws<AblyException>(delegate { ably.Channels.Get("Test").Publish("test", true); });

            error.ErrorInfo.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            error.ErrorInfo.Code.Should().Be(40100);
        }
    }
}
