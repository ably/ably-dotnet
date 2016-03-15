using System;
using Xunit;

namespace Ably.Tests
{
    public class TokenRequestDataTests
    {
        private const string ApiKey = "123.456:789";
        public readonly DateTimeOffset Now = new DateTime(2012, 12, 12, 10, 10, 10, DateTimeKind.Utc).ToDateTimeOffset();

        private static string GetKeyId()
        {
            return ApiKey.Split(':')[0];
        }

        private static string GetKeyValue()
        {
            return ApiKey.Split(':')[1];
        }

        private static TokenRequest GetTokenRequest()
        {
            return new TokenRequest { KeyName = GetKeyId(), ClientId = "123", Capability = new Capability(), Ttl = TimeSpan.FromMinutes(10) };
        }

        /// <summary>
        /// Initializes a new instance of the TokenRequestDataTests class.
        /// </summary>
        public TokenRequestDataTests()
        {
            Config.Now = () => Now;
        }

        [Fact]
        public void GetPostData_SetsCorrectId()
        {
            var tokenRequest = GetTokenRequest();

            var data = tokenRequest.GetPostData(GetKeyValue());

            Assert.Equal(tokenRequest.KeyName, data.keyName);
        }

        [Fact]
        public void GetPostData_SetsCorrectTtl()
        {
            var request = GetTokenRequest();
            var data = request.GetPostData(GetKeyValue());

            var expectedTtl = request.Ttl.Value.TotalMilliseconds;

            Assert.Equal(expectedTtl.ToString(), data.ttl);
        }

        [Fact]
        public void GetPostData_WhenTtlIsNotSet_SetsItToOneHourFromNow()
        {
            var request = new TokenRequest { KeyName = "123", Capability = new Capability() };
            var data = request.GetPostData(GetKeyValue());

            var expectedTtl = TimeSpan.FromHours(1).TotalMilliseconds;

            Assert.Equal(expectedTtl.ToString(), data.ttl);
        }

        [Fact]
        public void GetPostData_SetsCorrectCapability()
        {
            var request = GetTokenRequest();
            var data = request.GetPostData(GetKeyValue());

            Assert.Equal(request.Capability.ToJson(), data.capability);
        }

        [Fact]
        public void GetPostData_SetsCorrectClientId()
        {
            var request = GetTokenRequest();
            var data = request.GetPostData(GetKeyValue());

            Assert.Equal(request.ClientId, data.clientId);
        }

        [Fact]
        public void GetPostData_DataObjectHasCorrectTimestamp()
        {
            var request = GetTokenRequest();
            var data = request.GetPostData(GetKeyValue());

            Assert.Equal(Now.ToUnixTimeInMilliseconds().ToString(), data.timestamp);
        }

        [Fact]
        public void GetPostData_AlwaysHasRandomNonce()
        {
            var request = GetTokenRequest();

            var currentNonce = "";
            for (int i = 0; i < 10; i++)
            {
                var data = request.GetPostData(GetKeyValue());
                Assert.NotEqual(currentNonce, data.nonce);
                currentNonce = data.nonce;
            }
        }

        [Fact]
        public void GetPostData_HasMacBasedOnValueAndKey()
        {
            var request = GetTokenRequest();
            var data = request.GetPostData(GetKeyValue());
            var values = new[]
            {
                data.keyName,
                data.ttl,
                data.capability,
                data.clientId,
                data.timestamp,
                data.nonce
            };
            var signText = string.Join("\n", values) + "\n";

            string mac = Encryption.Crypto.ComputeHMacSha256(signText, GetKeyValue());

            Assert.Equal(mac, data.mac);
        }
    }
}