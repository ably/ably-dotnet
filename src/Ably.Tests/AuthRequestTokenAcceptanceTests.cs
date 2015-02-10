using System;
using Xunit;

namespace Ably.Tests
{
    public class AuthRequestTokenAcceptanceTests
    {
        private const string ApiKey = "AHSz6w.uQXPNQ:FGBZbsKSwqbCpkob";
        internal AblyRequest CurrentRequest { get; set; }
        public readonly DateTime Now = new DateTime(2012, 12, 12, 10, 10, 10, DateTimeKind.Utc);
        private readonly string _dummyTokenResponse = "{ \"access_token\": {}}";

        private RestClient GetRestClient()
        {
            var rest = new RestClient(new AblyOptions() { Key = ApiKey, UseBinaryProtocol = false});
            rest.ExecuteHttpRequest = (request) =>
            {
                CurrentRequest = request;
                return new AblyResponse() {TextResponse = _dummyTokenResponse};
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
                return new AblyResponse() {TextResponse = _dummyTokenResponse};
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
            var timeStamp = new DateTime(2015, 1, 1).ToDateTimeOffset();
            var tokenRequest = new TokenRequest {Timestamp = timeStamp};
            RequestToken(tokenRequest, null,
                (data, request) => Assert.Equal(timeStamp.ToUnixTime().ToString(), data.timestamp));
        }

        [Fact]
        public void WithOverridingTtl_OverridesTheDefault()
        {
            RequestToken(new TokenRequest {Ttl = TimeSpan.FromSeconds(2)}, null,
                (data, request) => Assert.Equal(TimeSpan.FromSeconds(2).TotalSeconds.ToString(), data.ttl));
        }

        [Fact]
        public void WithKeyIdAndKeySecret_PassesKeyIdAndUsesKeySecretToSignTheRequest()
        {
            var keyId = "Blah";
            var keyValue = "BBB";

            RequestToken(new TokenRequest(), new AuthOptions() {KeyId = keyId, KeyValue = keyValue}, (data, request) =>
            {
                Assert.Contains(keyId, request.Url);
                var values = new[]
                {
                    data.id,
                    data.ttl,
                    data.capability,
                    data.clientId,
                    data.timestamp,
                    data.nonce
                };

                var signText = string.Join("\n", values) + "\n";
                var expectedResult = signText.ComputeHMacSha256(keyValue);
                Assert.Equal(expectedResult, data.mac);
            });
        }
    }
}