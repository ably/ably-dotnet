using FluentAssertions;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    [Collection("AblyRest SandBox Collection")]
    [Trait("requires", "sandbox")]
    public class AuthSandboxSpecs : SandboxSpecs
    {
        public AuthSandboxSpecs(AblySandboxFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }

        static TokenParams CreateTokenParams(Capability capability, TimeSpan? ttl = null)
        {
            var res = new TokenParams();
            res.ClientId = "John";
            res.Capability = capability;
            if (ttl.HasValue)
                res.Ttl = ttl.Value;
            return res;
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA8a")]
        public async Task ShouldReturnTheRequestedToken(Protocol protocol)
        {
            var ttl = TimeSpan.FromSeconds(30 * 60);
            var capability = new Capability();
            capability.AddResource("foo").AllowPublish();

            var ably = await GetRestClient(protocol);
            var options = ably.Options;

            var token = await ably.Auth.RequestTokenAsync(CreateTokenParams(capability, ttl), null);

            var key = options.ParseKey();
            var appId = key.KeyName.Split('.').First();
            token.Token.Should().MatchRegex($@"^{appId}\.[\w-]+$");
            token.Issued.Should().BeWithin(TimeSpan.FromSeconds(30)).Before(DateTimeOffset.UtcNow);
            token.Expires.Should().BeWithin(TimeSpan.FromSeconds(30)).Before(DateTimeOffset.UtcNow + ttl);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA3a")]
        public async Task WithTokenId_AuthenticatesSuccessfullyOverHttpAndHttps(Protocol protocol)
        {
            var capability = new Capability();
            capability.AddResource("foo").AllowPublish();

            var ably = await GetRestClient(protocol);
            var token = await ably.Auth.RequestTokenAsync(CreateTokenParams(capability), null);

            var options = await Fixture.GetSettings();
            var httpTokenAbly = new AblyRest(new ClientOptions { Token = token.Token, Environment = options.Environment, Tls = false});
            var httpsTokenAbly = new AblyRest(new ClientOptions { Token = token.Token, Environment = options.Environment, Tls = true });

            //If it doesn't throw we are good :)
            await httpTokenAbly.Channels.Get("foo").PublishAsync("test", "true");
            await httpsTokenAbly.Channels.Get("foo").PublishAsync("test", "true");
        }

        [Theory]
        [ProtocolData]
        public async Task WithTokenId_WhenTryingToPublishToUnspecifiedChannel_ThrowsAblyException(Protocol protocol)
        {
            var capability = new Capability();
            capability.AddResource("foo").AllowPublish();

            var ably = await GetRestClient(protocol);

            var token = ably.Auth.RequestTokenAsync(CreateTokenParams(capability), null).Result;

            var tokenAbly = new AblyRest(new ClientOptions { Token = token.Token, Environment = AblyEnvironment.Sandbox });

            var error = await Assert.ThrowsAsync<AblyException>(() => tokenAbly.Channels.Get("boo").PublishAsync("test", "true"));
            error.ErrorInfo.code.Should().Be(40160);
            error.ErrorInfo.statusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [ProtocolData]
        public async Task WithInvalidTimeStamp_Throws(Protocol protocol)
        {
            var ably = await GetRestClient(protocol);

            var error = await Assert.ThrowsAsync<AblyException>(() =>
            {
                var tokenParams = CreateTokenParams(null);
                tokenParams.Timestamp = DateTimeOffset.UtcNow.AddDays(-1);
                return ably.Auth.RequestTokenAsync(tokenParams, new AuthOptions() { QueryTime = false});
            });

            error.ErrorInfo.code.Should().Be(40101);
            error.ErrorInfo.statusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA7a2")]
        public async Task WithClientId_RequestsATokenOnFirstMessageWithCorrectDefaults(Protocol protocol)
        {
            var ably = await GetRestClient(protocol, ablyOptions => ablyOptions.ClientId = "123");
            var channel = ably.Channels.Get("test");
            await channel.PublishAsync("test", "true");

            var token = ably.AblyAuth.CurrentToken;

            token.Should().NotBeNull();
            token.ClientId.Should().Be("123");
            token.Expires.Should().BeWithin(TimeSpan.FromSeconds(20)).Before(DateTimeOffset.UtcNow + Defaults.DefaultTokenTtl);
            token.Capability.ToJson().Should().Be(Defaults.DefaultTokenCapability.ToJson());
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA7b2")]
        [Trait("spec", "RSA10a")]
        public async Task WithoutClientId_WhenAuthorisedWithTokenParamsWithClientId_SetsClientId(Protocol protocol)
        {
            var ably = await GetRestClient(protocol);
            await ably.Auth.AuthoriseAsync(new TokenParams() {ClientId = "123"}, new AuthOptions() { Force = true});
            ably.AblyAuth.GetClientId().Should().Be("123");
            ably.AblyAuth.AuthMethod.Should().Be(AuthMethod.Token);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA8f1")]
        public async Task TokenAuthWithouthClientId_ShouldNotSetClientIdOnMessagesAndTheClient(Protocol protocol)
        {
            var client = await GetRestClient(protocol, opts => opts.QueryTime = true);
            var settings = await Fixture.GetSettings();
            var token = await client.Auth.RequestTokenAsync();
            var tokenClient = new AblyRest(new ClientOptions
            {
                TokenDetails = token,
                Environment = settings.Environment,
                UseBinaryProtocol = protocol == Protocol.MsgPack
            });

            tokenClient.AblyAuth.GetClientId().Should().BeNullOrEmpty();
            var channel = tokenClient.Channels["persisted:test".AddRandomSuffix()];
            await channel.PublishAsync("test", "test");
            var message = (await channel.HistoryAsync()).First();
            message.clientId.Should().BeNullOrEmpty();
            message.data.Should().Be("test");
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA8f2")]
        public async Task TokenAuthWithouthClientIdAndAMessageWithExplicitId_ShouldThrow(Protocol protocol)
        {
            var client = await GetRestClient(protocol);
            var settings = await Fixture.GetSettings();
            var token = await client.Auth.RequestTokenAsync();
            var tokenClient = new AblyRest(new ClientOptions
            { TokenDetails = token, Environment = settings.Environment, UseBinaryProtocol = protocol == Protocol.MsgPack });
            await Assert.ThrowsAsync<AblyException>(() => tokenClient.Channels["test"].PublishAsync(new Message("test", "test") { clientId = "123"}));
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA8f3")]
        public async Task TokenAuthWithWildcardClientId_ShouldPublishMessageSuccessufflyAndClientIdShouldBeSetToWildcard(Protocol protocol)
        {
            var client = await GetRestClient(protocol);
            var settings = await Fixture.GetSettings();
            var token = await client.Auth.RequestTokenAsync(new TokenParams() { ClientId = "*"});
            var tokenClient = new AblyRest(new ClientOptions
            { TokenDetails = token, Environment = settings.Environment, UseBinaryProtocol = protocol == Protocol.MsgPack });

            var channel = tokenClient.Channels["pesisted:test"];
            await channel.PublishAsync("test", "test");
            tokenClient.AblyAuth.GetClientId().Should().Be("*");
            var message = (await channel.HistoryAsync()).First();
            message.clientId.Should().BeNullOrEmpty();
            message.data.Should().Be("test");
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA8f4")]
        public async Task TokenAuthWithWildcardClientId_WhenPublishingMessageWithClientId_ShouldExpectClientIdToBeSentWithTheMessage(Protocol protocol)
        {
            var client = await GetRestClient(protocol);
            var settings = await Fixture.GetSettings();
            var token = await client.Auth.RequestTokenAsync(new TokenParams() { ClientId = "*" });
            var tokenClient = new AblyRest(new ClientOptions
            { TokenDetails = token, Environment = settings.Environment, UseBinaryProtocol = protocol == Protocol.MsgPack });

            var channel = tokenClient.Channels["pesisted:test"];
            await channel.PublishAsync(new Message("test", "test") { clientId = "123"});
            tokenClient.AblyAuth.GetClientId().Should().Be("*");
            var message = (await channel.HistoryAsync()).First();
            message.clientId.Should().Be("123");
            message.data.Should().Be("test");
        }

        [Theory]
        [ProtocolData]
        public async Task TokenAuthUrlWhenPlainTextTokenIsReturn_ShouldBeAblyToPublishWithNewToken(Protocol protocol)
        {
            var client = await GetRestClient(protocol);
            var token = await client.Auth.RequestTokenAsync(new TokenParams() { ClientId = "*"});
            var settings = await Fixture.GetSettings();
            var authUrl = "http://echo.ably.io/?type=text&body=" + token.Token;

            var authUrlClient = new AblyRest(new ClientOptions
            {
                AuthUrl = new Uri(authUrl),
                Environment = settings.Environment,
                UseBinaryProtocol = protocol == Protocol.MsgPack
            });

            var channel = authUrlClient.Channels["pesisted:test"];
            await channel.PublishAsync(new Message("test", "test") { clientId = "123" });

            var message = (await channel.HistoryAsync()).First();
            message.clientId.Should().Be("123");
            message.data.Should().Be("test");
        }

        [Theory]
        [ProtocolData]
        public async Task TokenAuthUrlWithJsonTokenReturned_ShouldBeAblyToPublishWithNewToken(Protocol protocol)
        {
            var client = await GetRestClient(protocol);
            var token = await client.Auth.RequestTokenAsync(new TokenParams() { ClientId = "*" });
            var settings = await Fixture.GetSettings();
            var authUrl = "http://echo.ably.io/?type=json&body=" + Uri.EscapeUriString(token.ToJson());

            var authUrlClient = new AblyRest(new ClientOptions
            {
                AuthUrl = new Uri(authUrl),
                Environment = settings.Environment,
                UseBinaryProtocol = protocol == Protocol.MsgPack
            });

            var channel = authUrlClient.Channels["pesisted:test"];
            await channel.PublishAsync(new Message("test", "test") { clientId = "123" });

            var message = (await channel.HistoryAsync()).First();
            message.clientId.Should().Be("123");
            message.data.Should().Be("test");
        }

        public class RequestTokenSpecs : AuthSandboxSpecs
        {
            public RequestTokenSpecs(AblySandboxFixture fixture, ITestOutputHelper output) : base(fixture, output)
            {
            }
        }
    }
}