using System;
using FluentAssertions;
using IO.Ably.Encryption;
using Xunit;

namespace IO.Ably.Tests
{
    public class TokenRequestPopulateTests
    {
        private const string ApiKey = "123.456:789";
        public readonly DateTimeOffset Now = DateHelper.CreateDate(2012, 12, 12, 10, 10, 10);

        private static string GetKeyId()
        {
            return ApiKey.Split(':')[0];
        }

        private static string GetKeyValue()
        {
            return ApiKey.Split(':')[1];
        }

        private static TokenParams GetTokenParams()
        {
            return new TokenParams() { ClientId = "123", Capability = new Capability(), Ttl = TimeSpan.FromMinutes(10) };
        }

        private TokenRequest _request;

        private TokenRequest Populate(TokenParams tokenParams)
        {
            return _request.Populate(tokenParams, GetKeyId(), GetKeyValue());
        }

        /// <summary>
        /// Initializes a new instance of the TokenRequestDataTests class.
        /// </summary>
        public TokenRequestPopulateTests()
        {
            Config.Now = () => Now;
            _request = new TokenRequest();
        }

        [Fact]
        public void Populate_SetsCorrectId()
        {
            var tokenParams = GetTokenParams();

            var data = _request.Populate(tokenParams, "123", GetKeyValue());

            data.KeyName.Should().Be("123");
        }

        [Fact]
        public void GetPostData_SetsCorrectTtl()
        {
            var tokenParams = GetTokenParams();

            var request = Populate(tokenParams);
            var expectedTtl = tokenParams.Ttl.Value;

            request.Ttl.Should().Be(expectedTtl);
        }

        [Fact]
        public void GetPostData_WhenTtlIsNotSet_SetsItToOneHourFromNow()
        {
            var tokenParams = new TokenParams();
            var request = Populate(tokenParams);

            var expectedTtl = TimeSpan.FromHours(1);

            request.Ttl.Should().Be(expectedTtl);
        }

        [Fact]
        public void GetPostData_SetsCorrectCapability()
        {
            var tokenParams = GetTokenParams();
            var request = Populate(tokenParams);

            Assert.Equal(tokenParams.Capability, request.Capability);
        }

        [Fact]
        public void GetPostData_SetsCorrectClientId()
        {
            var tokenParams = GetTokenParams();
            var request = Populate(tokenParams);

            request.ClientId.Should().Be(tokenParams.ClientId);
        }

        [Fact]
        public void GetPostData_DataObjectHasCorrectTimestamp()
        {
            var tokenParams = GetTokenParams();
            var request = Populate(tokenParams);

            request.Timestamp.Should().Be(Now);
        }

        [Fact]
        public void GetPostData_AlwaysHasRandomNonce()
        {
            var currentNonce = "";
            for (int i = 0; i < 10; i++)
            {
                var request = new TokenRequest();
                request.Nonce.Should().NotBe(currentNonce);
                currentNonce = request.Nonce;
            }
        }

        [Fact]
        public void GetPostData_HasMacBasedOnValueAndKey()
        {
            var tokenParams = GetTokenParams();
            var request = Populate(tokenParams);
            var values = new[]
            {
                request.KeyName,
                request.Ttl?.TotalMilliseconds.ToString(),
                request.Capability.ToJson(),
                request.ClientId,
                request.Timestamp?.ToUnixTimeInMilliseconds().ToString(),
                request.Nonce
            };
            var signText = string.Join("\n", values) + "\n";

            string mac = Crypto.ComputeHMacSha256(signText, GetKeyValue());

            Assert.Equal(mac, request.Mac);
        }
    }
}