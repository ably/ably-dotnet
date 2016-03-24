using System;
using Xunit;

namespace IO.Ably.Tests
{
    public class AuthRequestTokenAcceptanceTests
    {
        private const string ApiKey = "123.456:789";
        internal AblyRequest CurrentRequest { get; set; }
        public readonly DateTime Now = new DateTime(2012, 12, 12, 10, 10, 10, DateTimeKind.Utc);
        private readonly string _dummyTokenResponse = "{ \"access_token\": {}}";

        private AblyRest GetRestClient()
        {
            var rest = new AblyRest(new AblyOptions() { Key = ApiKey, UseBinaryProtocol = false});
            rest.ExecuteHttpRequest = (request) =>
            {
                CurrentRequest = request;
                return _dummyTokenResponse.response();
            };

            Config.Now = () => Now;
            return rest;
        }

        private void RequestToken(TokenRequest request, AuthOptions authOptions,
            Action<TokenRequestPostData, AblyRequest> action)
        {
            var rest = GetRestClient();

            rest.ExecuteHttpRequest = x =>
            {
                //Assert
                var data = x.PostData as TokenRequestPostData;
                action(data, x);
                return _dummyTokenResponse.response();
            };

            rest.Auth.RequestToken(request, authOptions);
        }

        [Fact]
        public void WithOverridingClientId_OverridesTheDefault()
        {
            var tokenRequest = new TokenRequest {ClientId = "123"};
            RequestToken(tokenRequest, null, (data, request) => Assert.Equal("123", data.clientId));
        }

        [Fact]
        public void WithOverridingCapability_OverridesTheDefault()
        {
            var capability = new Capability();
            capability.AddResource("test").AllowAll();
            var tokenRequest = new TokenRequest {Capability = capability};

            RequestToken(tokenRequest, null, (data, request) => Assert.Equal(capability.ToJson(), data.capability));
        }

        [Fact]
        public void WithOverridingNonce_OverridesTheDefault()
        {
            RequestToken(new TokenRequest {Nonce = "Blah"}, null, (data, request) => Assert.Equal("Blah", data.nonce));
        }

        [Fact]
        public void WithOverridingTimeStamp_OverridesTheDefault()
        {
            var timeStamp = DateTime.SpecifyKind(new DateTime(2015, 1, 1), DateTimeKind.Utc);
            var tokenRequest = new TokenRequest {Timestamp = timeStamp};
            RequestToken(tokenRequest, null,
                (data, request) => Assert.Equal(timeStamp.ToUnixTimeInMilliseconds().ToString(), data.timestamp));
        }

        [Fact]
        public void WithOverridingTtl_OverridesTheDefault()
        {
            RequestToken(new TokenRequest {Ttl = TimeSpan.FromSeconds(2)}, null,
                (data, request) => Assert.Equal(TimeSpan.FromSeconds(2).TotalMilliseconds.ToString(), data.ttl));
        }
    }
}