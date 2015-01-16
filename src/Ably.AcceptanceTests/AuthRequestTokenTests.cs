using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace Ably.AcceptanceTests
{
    public class AuthRequestTokenTests
    {

        [Test]
        public void ShouldReturnTheRequestedToken()
        {
            //Arrange
            var ttl = TimeSpan.FromSeconds(30*60);
            var capability = new Capability();
            capability.AddResource("foo").AllowPublish();

            var options = TestsSetup.GetDefaultOptions();
            var ably = new Rest(options);

            //Act
            var token = ably.Auth.RequestToken(new TokenRequest { Capability = capability, Ttl = ttl }, options);

            //Assert

            token.Id.Should().MatchRegex(string.Format(@"^{0}\.[\w-]+$", options.AppId));
            token.KeyId.Should().Be(options.KeyId);
            token.IssuedAt.Should().BeWithin(TimeSpan.FromSeconds(2)).Before(DateTime.Now);
            token.ExpiresAt.Should().BeWithin(TimeSpan.FromSeconds(2)).Before(DateTime.Now  + ttl);
        }

        [Test]
        public void WithTokenId_AuthenticatesSuccessfully()
        {
            //Arrange
            var capability = new Capability();
            capability.AddResource("foo").AllowPublish();

            var options = TestsSetup.GetDefaultOptions();
            var ably = new Rest(options);
            var token = ably.Auth.RequestToken(new TokenRequest() { Capability = capability }, null);

            var tokenAbly = new Rest(new AblyOptions {AuthToken = token.Id, Environment = TestsSetup.TestData.Environment});

            //Act & Assert
            Assert.DoesNotThrow(delegate { tokenAbly.Channels.Get("foo").Publish("test", true); });
        }

        [Test]
        public void WithTokenId_WhenTryingToPublishToUnspecifiedChannel_ThrowsAblyException()
        {
            //Arrange
            var capability = new Capability();
            capability.AddResource("foo").AllowPublish();

            var options = TestsSetup.GetDefaultOptions();
            var ably = new Rest(options);
            var token = ably.Auth.RequestToken(new TokenRequest() { Capability = capability }, null);

            var tokenAbly = new Rest(new AblyOptions { AuthToken = token.Id , Environment = AblyEnvironment.Sandbox});

            //Act & Assert
            var error = Assert.Throws<AblyException>(delegate { tokenAbly.Channels.Get("boo").Publish("test", true); });
            error.ErrorInfo.Code.Should().Be(40160);
            error.ErrorInfo.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Test]
        public void WithInvalidTimeStamp_Throws()
        {
            //Arrange
            var options = TestsSetup.GetDefaultOptions();
            var ably = new Rest(options);
            
            //Act
            var error = Assert.Throws<AblyException>(
                delegate { ably.Auth.RequestToken(new TokenRequest() { Timestamp = DateTime.UtcNow.AddDays(-1)}, null); });

            //Assert
            error.ErrorInfo.Code.Should().Be(40101);
            error.ErrorInfo.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Test]
        public void WithClientId_RequestsATokenOnFirstMessageWithCorrectDefaults()
        {
            //Arrange
            var options = TestsSetup.GetDefaultOptions();
            options.ClientId = "123";
            var ably = new Rest(options);
            
            ably.Channels.Get("test").Publish("test", true);

            var token = ably.CurrentToken;

            token.ClientId.Should().Be("123");
            token.ExpiresAt.Should().BeWithin(TimeSpan.FromSeconds(2)).Before(DateTime.UtcNow + TokenRequest.Defaults.Ttl);
            token.Capability.ToJson().Should().Be(TokenRequest.Defaults.Capability.ToJson());
        }
    }


}
