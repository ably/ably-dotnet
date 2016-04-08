using System;
using FluentAssertions;
using IO.Ably.Transport;
using Xunit;

namespace IO.Ably.Tests
{
    public class AuthCreateTokenRequestAcceptanceTests
    {
        private const string ApiKey = "123.456:789";
        internal AblyRequest CurrentRequest { get; set; }
        public readonly DateTime Now = new DateTime(2012, 12, 12, 10, 10, 10, DateTimeKind.Utc);
        public AblyRest Client { get; set; }

        public AuthCreateTokenRequestAcceptanceTests()
        {
            Client = GetRestClient();
        }

        private AblyRest GetRestClient()
        {
            var rest = new AblyRest(new ClientOptions() { Key = ApiKey, UseBinaryProtocol = false});
            rest.ExecuteHttpRequest = (request) =>
            {
                CurrentRequest = request;
                return "{}".ToAblyResponse();
            };

            Config.Now = () => Now;
            return rest;
        }

        [Fact]
        public void UsesKeyIdFromTheClient()
        {
            var data = Client.Auth.CreateTokenRequest(null, null).Result;
            data.KeyName.Should().Be(Client.Options.ParseKey().KeyName);
        }

        [Fact]
        public void UsesDefaultTtlWhenNoneIsSpecified()
        {
            var data = Client.Auth.CreateTokenRequest(null, null).Result;
            data.Ttl.Should().Be(Defaults.DefaultTokenTtl.TotalMilliseconds.ToString());
        }

        [Fact]
        public void UsesTheDefaultCapability()
        {
            var data = Client.Auth.CreateTokenRequest(null, null).Result;
            data.Capability.Should().Be(Defaults.DefaultTokenCapability.ToJson());
        }

        [Fact]
        public void UsesUniqueNonseWhichIsMoreThan16Characters()
        {
            var data = Client.Auth.CreateTokenRequest(null, null).Result;
            var secondTime = Client.Auth.CreateTokenRequest(null, null).Result;
            data.Nonce.Should().NotBe(secondTime.Nonce);
            data.Nonce.Length.Should().BeGreaterOrEqualTo(16);
        }

        [Fact]
        public void WithCapabilityOverridesDefault()
        {
            var capability = new Capability();
            capability.AddResource("test").AllowAll();

            var data = Client.Auth.CreateTokenRequest(new TokenParams {Capability = capability}, null).Result;
            data.Capability.Should().Be(capability.ToJson());
        }

        [Fact]
        public void WithTtlOverridesDefault()
        {
            var data = Client.Auth.CreateTokenRequest(new TokenParams {Ttl = TimeSpan.FromHours(2)}, null).Result;

            data.Ttl.Should().Be(TimeSpan.FromHours(2).TotalMilliseconds.ToString());
        }

        [Fact]
        public void WithNonceOverridesDefault()
        {
            var data = Client.Auth.CreateTokenRequest(new TokenParams() { Nonce = "Blah" }, null).Result;
            data.Nonce.Should().Be("Blah");
        }

        [Fact]
        public void WithTimeStampOverridesDefault()
        {
            var date = DateTime.SpecifyKind(new DateTime(2014, 1, 1), DateTimeKind.Utc);
            var data = Client.Auth.CreateTokenRequest(new TokenParams() { Timestamp= date }, null).Result;
            data.Timestamp.Should().Be(date.ToUnixTimeInMilliseconds().ToString());
        }

        [Fact]
        public void WithClientIdOverridesDefault()
        {
            var data = Client.Auth.CreateTokenRequest(new TokenParams() { ClientId = "123"}, null).Result;
            data.ClientId.Should().Be("123");
        }

        [Fact]
        public void WithQueryTimeQueriesForTimestamp()
        {
            var currentTime = Config.Now().ToUnixTimeInMilliseconds();
            Client.ExecuteHttpRequest = x => ( "[" + currentTime + "]" ).ToAblyJsonResponse();
            var data = Client.Auth.CreateTokenRequest(null, new AuthOptions() {QueryTime = true}).Result;
            data.Timestamp.Should().Be(currentTime.ToString());
        }

        [Fact]
        public async void WithOutKeyIdThrowsException()
        {
            var client = new AblyRest(new ClientOptions());
            await Assert.ThrowsAsync<AblyException>(() => client.Auth.CreateTokenRequest(null, null));
        }

        [Fact]
        public async void WithOutKeyValueThrowsException()
        {
            var client = new AblyRest(new ClientOptions() { Key = "111.222"});
            await Assert.ThrowsAsync<AblyException>(() => client.Auth.CreateTokenRequest(null, null));
        }

        [Fact]
        public void GeneratesHMac()
        {
            var data = Client.Auth.CreateTokenRequest(null, null).Result;
            data.Mac.Should().NotBeEmpty();
        }
    }
}