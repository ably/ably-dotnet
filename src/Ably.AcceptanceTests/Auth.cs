using System;
using System.Collections.Generic;
using System.Linq;
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
        [TestCase("client_id")]
        [TestCase("capability")]
        [TestCase("nonce")]
        [TestCase("timestamp")]
        [TestCase("ttl")]
        public void WithOption_ShouldOverrideDefault(string optionName)
        {
            //Arrange
            

            //Act

            //Assert
        }
    }


}
