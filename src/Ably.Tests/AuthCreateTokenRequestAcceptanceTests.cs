using System;
using FluentAssertions;
using FluentAssertions.Common;
using Xunit;

namespace IO.Ably.Tests
{
    public class AuthCreateTokenRequestAcceptanceTests
    {
        private const string ApiKey = "123.456:789";
        internal AblyRequest CurrentRequest { get; set; }
        public readonly DateTime Now = new DateTime(2012, 12, 12, 10, 10, 10, DateTimeKind.Utc);
        public RestClient Client { get; set; }

        public AuthCreateTokenRequestAcceptanceTests()
        {
            Client = GetRestClient();
        }

        private RestClient GetRestClient()
        {
            var rest = new RestClient(new AblyOptions() { Key = ApiKey, UseBinaryProtocol = false});
            rest.ExecuteHttpRequest = (request) =>
            {
                CurrentRequest = request;
                return new AblyResponse() {TextResponse = "{}"};
            };

            Config.Now = () => Now;
            return rest;
        }

        [Fact]
        public void UsesKeyIdFromTheClient()
        {
            var data = Client.Auth.CreateTokenRequest(null, null);
            data.keyName.Should().Be(Client.Options.ParseKey().KeyName);
        }

        [Fact]
        public void UsesDefaultTtlWhenNoneIsSpecified()
        {
            var data = Client.Auth.CreateTokenRequest(null, null);
            data.ttl.Should().Be(TokenRequest.Defaults.Ttl.TotalMilliseconds.ToString());
        }

        [Fact]
        public void UsesTheDefaultCapability()
        {
            var data = Client.Auth.CreateTokenRequest(null, null);
            data.capability.Should().Be(TokenRequest.Defaults.Capability.ToJson());
        }

        [Fact]
        public void UsesUniqueNonseWhichIsMoreThan16Characters()
        {
            var data = Client.Auth.CreateTokenRequest(null, null);
            var secondTime = Client.Auth.CreateTokenRequest(null, null);
            data.nonce.Should().NotBe(secondTime.nonce);
            data.nonce.Length.Should().BeGreaterOrEqualTo(16);
        }

        [Fact]
        public void WithCapabilityOverridesDefault()
        {
            var capability = new Capability();
            capability.AddResource("test").AllowAll();

            var data = Client.Auth.CreateTokenRequest(new TokenRequest() {Capability = capability}, null);
            data.capability.Should().Be(capability.ToJson());
        }

        [Fact]
        public void WithTtlOverridesDefault()
        {
            var data = Client.Auth.CreateTokenRequest(new TokenRequest() {Ttl = TimeSpan.FromHours(2)}, null);

            data.ttl.Should().Be(TimeSpan.FromHours(2).TotalMilliseconds.ToString());
        }

        [Fact]
        public void WithNonceOverridesDefault()
        {
            var data = Client.Auth.CreateTokenRequest(new TokenRequest() { Nonce = "Blah" }, null);
            data.nonce.Should().Be("Blah");
        }

        [Fact]
        public void WithTimeStampOverridesDefault()
        {
            var date = new DateTime(2014, 1, 1).ToDateTimeOffset();
            var data = Client.Auth.CreateTokenRequest(new TokenRequest() { Timestamp= date }, null);
            data.timestamp.Should().Be(date.ToUnixTimeInMilliseconds().ToString());
        }

        [Fact]
        public void WithClientIdOverridesDefault()
        {
            var data = Client.Auth.CreateTokenRequest(new TokenRequest() { ClientId = "123"}, null);
            data.clientId.Should().Be("123");
        }

        [Fact]
        public void WithQueryTimeQueriesForTimestamp()
        {
            var currentTime = Config.Now().ToUnixTimeInMilliseconds();
            Client.ExecuteHttpRequest = x => {
                return new AblyResponse { TextResponse = "[" + currentTime + "]", Type = ResponseType.Json }; };
            var data = Client.Auth.CreateTokenRequest(null, new AuthOptions() {QueryTime = true});
            data.timestamp.Should().Be(currentTime.ToString());
        }

        [Fact]
        public void WithOutKeyIdThrowsException()
        {
            var client = new RestClient(new AblyOptions());
            Assert.Throws<AblyException>(delegate { client.Auth.CreateTokenRequest(null, null); });
        }

        [Fact]
        public void WithOutKeyValueThrowsException()
        {
            var client = new RestClient(new AblyOptions() { Key = "111.222"});
            Assert.Throws<AblyException>(delegate { client.Auth.CreateTokenRequest(null, null); });
        }

        [Fact]
        public void GeneratesHMac()
        {
            var data = Client.Auth.CreateTokenRequest(null, null);
            data.mac.Should().NotBeEmpty();
        }
    }
}