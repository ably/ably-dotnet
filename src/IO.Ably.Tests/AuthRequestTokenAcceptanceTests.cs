using System;
using Xunit;

namespace IO.Ably.Tests
{
    public class AuthRequestTokenAcceptanceTests
    {
        private const string ApiKey = "123.456:789";
        internal AblyRequest CurrentRequest { get; set; }
        public readonly DateTimeOffset Now = new DateTimeOffset(2012, 12, 12, 10, 10, 10, TimeSpan.Zero);
        private readonly string _dummyTokenResponse = "{ \"access_token\": {}}";

        private AblyRest GetRestClient()
        {
            var rest = new AblyRest(new ClientOptions() { Key = ApiKey, UseBinaryProtocol = false });
            rest.ExecuteHttpRequest = (request) =>
            {
                CurrentRequest = request;
                return _dummyTokenResponse.ToAblyResponse();
            };

            Config.Now = () => Now;
            return rest;
        }

        private void RequestToken(TokenParams tokenParams, AuthOptions authOptions,
            Action<TokenRequest, AblyRequest> action)
        {
            var rest = GetRestClient();

            rest.ExecuteHttpRequest = x =>
            {
                //Assert
                var data = x.PostData as TokenRequest;
                action(data, x);
                return _dummyTokenResponse.ToAblyResponse();
            };

            rest.Auth.RequestToken(tokenParams, authOptions);
        }

        [Fact]
        public void WithOverridingClientId_OverridesTheDefault()
        {
            var tokenParams = new TokenParams { ClientId = "123" };
            RequestToken(tokenParams, null, (data, request) => Assert.Equal("123", data.ClientId));
        }

        [Fact]
        public void WithOverridingCapability_OverridesTheDefault()
        {
            var capability = new Capability();
            capability.AddResource("test").AllowAll();
            var tokenParams = new TokenParams { Capability = capability };

            RequestToken(tokenParams, null, (data, request) => Assert.Equal(capability.ToJson(), data.Capability));
        }

        [Fact]
        public void WithOverridingNonce_OverridesTheDefault()
        {
            RequestToken(new TokenParams { Nonce = "Blah" }, null, (data, request) => Assert.Equal("Blah", data.Nonce));
        }

        [Fact]
        public void WithOverridingTimeStamp_OverridesTheDefault()
        {
            var timeStamp = new DateTimeOffset(2015, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var tokenParams = new TokenParams { Timestamp = timeStamp };
            RequestToken(tokenParams, null,
                (data, request) => Assert.Equal(timeStamp.ToUnixTimeInMilliseconds().ToString(), data.Timestamp));
        }

        [Fact]
        public void WithOverridingTtl_OverridesTheDefault()
        {
            RequestToken(new TokenParams { Ttl = TimeSpan.FromSeconds(2) }, null,
                (data, request) => Assert.Equal(TimeSpan.FromSeconds(2).TotalMilliseconds.ToString(), data.Ttl));
        }
    }
}