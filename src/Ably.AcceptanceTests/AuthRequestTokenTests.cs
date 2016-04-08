using FluentAssertions;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace IO.Ably.AcceptanceTests
{
    [TestFixture( Protocol.MsgPack )]
    [TestFixture( Protocol.Json )]
    public class AuthRequestTokenTests
    {
        private readonly bool _binaryProtocol;

        public AuthRequestTokenTests( Protocol binaryProtocol )
        {
            _binaryProtocol = binaryProtocol == Protocol.MsgPack;
        }

        static TokenRequest createTokenRequest( Capability capability, TimeSpan? ttl = null )
        {
            TokenRequest res = new TokenRequest();
            res.ClientId = "John";
            res.Capability = capability;
            if( ttl.HasValue )
                res.Ttl = ttl.Value;
            return res;
        }

        [Test]
        public void ShouldReturnTheRequestedToken()
        {
            var ttl = TimeSpan.FromSeconds(30*60);
            var capability = new Capability();
            capability.AddResource( "foo" ).AllowPublish();

            AblyRest ably = GetRestClient();
            var options = ably.Options;

            var token = ably.Auth.RequestToken(createTokenRequest( capability, ttl ), null).Result;

            var key = options.ParseKey();
            var appId = key.KeyName.Split('.').First();
            token.Token.Should().MatchRegex( string.Format( @"^{0}\.[\w-]+$", appId ) );
            token.KeyName.Should().Be( key.KeyName );
            token.Issued.Should().BeWithin( TimeSpan.FromSeconds( 30 ) ).Before( DateTime.UtcNow );
            token.Expires.Should().BeWithin( TimeSpan.FromSeconds( 30 ) ).Before( DateTime.UtcNow + ttl );
        }

        private AblyRest GetRestClient( Action<ClientOptions> action = null )
        {
            var options = TestsSetup.GetDefaultOptions();
            action?.Invoke(options);
            options.UseBinaryProtocol = _binaryProtocol;
            return new AblyRest( options );
        }

        [Test]
        public void WithTokenId_AuthenticatesSuccessfully()
        {
            var capability = new Capability();
            capability.AddResource( "foo" ).AllowPublish();

            var ably = GetRestClient();
            var token = ably.Auth.RequestToken(createTokenRequest( capability ), null).Result;

            var tokenAbly = new AblyRest(new ClientOptions {Token = token.Token, Environment = TestsSetup.TestData.Environment});

            Assert.DoesNotThrow( delegate { tokenAbly.Channels.Get( "foo" ).Publish( "test", true ); } );
        }

        [Test]
        public void WithTokenId_WhenTryingToPublishToUnspecifiedChannel_ThrowsAblyException()
        {
            var capability = new Capability();
            capability.AddResource( "foo" ).AllowPublish();

            var ably = GetRestClient();

            var token = ably.Auth.RequestToken(createTokenRequest(capability), null).Result;

            var tokenAbly = new AblyRest(new ClientOptions { Token = token.Token , Environment = AblyEnvironment.Sandbox});

            var error = Assert.ThrowsAsync<AblyException>(() => tokenAbly.Channels.Get("boo").Publish("test", true));
            error.ErrorInfo.code.Should().Be(40160);
            error.ErrorInfo.statusCode.Should().Be( HttpStatusCode.Unauthorized );
        }

        [Test]
        public void WithInvalidTimeStamp_Throws()
        {
            var options = TestsSetup.GetDefaultOptions();
            var ably = new AblyRest(options);

            var error = Assert.ThrowsAsync<AblyException>(() =>
            {
                TokenRequest req = createTokenRequest(null);
                req.Timestamp = DateTime.UtcNow.AddDays(-1);
                return ably.Auth.RequestToken(req, null);
            });

            error.ErrorInfo.code.Should().Be( 40101 );
            error.ErrorInfo.statusCode.Should().Be( HttpStatusCode.Unauthorized );
        }

        [Test]
        public async Task WithClientId_RequestsATokenOnFirstMessageWithCorrectDefaults()
        {
            var ably = GetRestClient(ablyOptions => ablyOptions.ClientId = "123");
            var channel = ably.Channels.Get( "test" );
            await channel.Publish("test", true);

            var token = ably.CurrentToken;

            token.Should().NotBeNull();
            token.ClientId.Should().Be( "123" );
            token.Expires.Should().BeWithin( TimeSpan.FromSeconds( 20 ) ).Before( DateTime.UtcNow + TokenRequest.Defaults.Ttl );
            token.Capability.ToJson().Should().Be( TokenRequest.Defaults.Capability.ToJson() );
        }
    }
}