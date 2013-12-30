using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Ably.Tests
{
    public class TokenRequestDataTests
    {
        private const string ApiKey = "AHSz6w.uQXPNQ:FGBZbsKSwqbCpkob";
        public readonly DateTime Now = new DateTime(2012, 12, 12, 10, 10, 10, DateTimeKind.Utc);

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
            return new TokenRequest { Id = GetKeyId(), ClientId = "123", Capability = new Capability(), Ttl = TimeSpan.FromMinutes(10) };
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

            Assert.Equal(tokenRequest.Id, data.id);
        }

        [Fact]
        public void GetPostData_SetsCorrectTtl()
        {
            var request = GetTokenRequest();
            var data = request.GetPostData(GetKeyValue());

            var expectedTtl = request.Ttl.Value.TotalSeconds;

            Assert.Equal(expectedTtl.ToString(), data.ttl);
        }

        [Fact]
        public void GetPostData_WhenTtlIsNotSet_SetsItToOneHourFromNow()
        {
            var request = new TokenRequest { Id = "123", Capability = new Capability() };
            var data = request.GetPostData(GetKeyValue());

            var expectedTtl = TimeSpan.FromHours(1).TotalSeconds;

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

            Assert.Equal(request.ClientId, data.client_id);
        }

        [Fact]
        public void GetPostData_DataObjectHasCorrectTimestamp()
        {
            var request = GetTokenRequest();
            var data = request.GetPostData(GetKeyValue());

            Assert.Equal(Now.ToUnixTime().ToString(), data.timestamp);
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
                data.id, 
                data.ttl,
                data.capability, 
                data.client_id, 
                data.timestamp,
                data.nonce
            };
            var signText = string.Join("\n", values) + "\n";

            string mac = signText.ComputeHMacSha256(GetKeyValue());

            Assert.Equal(mac, data.mac);
        }
    }
}
