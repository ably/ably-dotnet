using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Ably.Tests
{
    public class RequestTokenTests
    {
        private const string ApiKey = "AHSz6w:uQXPNQ:FGBZbsKSwqbCpkob";
        public AblyRequest CurrentRequest { get; set; }
        public readonly DateTime Now = new DateTime(2012, 12, 12, 10, 10, 10, DateTimeKind.Utc);

        private Rest GetRestClient()
        {
            var rest = new Rest(ApiKey);
            rest.ExecuteRequest = (request) =>
            {
                CurrentRequest = request;
                return null;
            };

            rest.Now = () => Now;
            return rest;
        }

        private static string GetKeyId()
        {
            return ApiKey.Split(':')[1];
        }

        private static string GetKeyValue()
        {
            return ApiKey.Split(':')[2];
        }


        [Fact]
        public void RequestToken_SendsRequestToCorrectUrl()
        {
            //Arrange
            SendRequestTokenWithValidOptions();

            //Assert
            Assert.Equal(CurrentRequest.Url, "/apps/AHSz6w/requestToken");
        }

        [Fact]
        public void RequestToken_WithoutExpires_SetsExpiresParamToOneHour()
        {
            var rest = GetRestClient();


            rest.RequestToken(new RequestTokenOptions { Capability = "Blah" });

            var expectedUnixTime = Now.AddHours(1).ToUnixTime().ToString();
            Assert.Equal(expectedUnixTime, CurrentRequest.PostParameters["expires"]);
        }

       
        
        [Fact]
        public void RequestToken_ShouldPostCorrectKeyId()
        {
            //Arrange
            SendRequestTokenWithValidOptions();

            //Assert

            Assert.Equal(CurrentRequest.PostParameters["id"],GetKeyId()); //TODO: Change
        }

        [Fact]
        public void RequestToken_ShouldPostCorrectExpiryTime()
        {
            var options = SendRequestTokenWithValidOptions();

            var expectedUnixTime = Now.Add(options.Expires.Value).ToUnixTime().ToString();
            Assert.Equal(expectedUnixTime, CurrentRequest.PostParameters["expires"]);

        }

        [Fact]
        public void RequestToken_ShouldPostCorrectCapability()
        {
            var options = SendRequestTokenWithValidOptions();

            Assert.Equal(options.Capability, CurrentRequest.PostParameters["capability"]);
        }

        [Fact]
        public void RequestToken_ShouldPostCorrectClientId()
        {
            var options = SendRequestTokenWithValidOptions();

            Assert.Equal(options.ClientId, CurrentRequest.PostParameters["client_id"]);
        }

        [Fact]
        public void RequestToken_WithOutClientId_DoesntIncludeItInPostRequest()
        {
            var rest = GetRestClient();

            rest.RequestToken(new RequestTokenOptions { Capability = "Test" });

            Assert.False(CurrentRequest.PostParameters.ContainsKey("client_id"));
        }

        [Fact]
        public void RequestToken_ShouldPostTimeStamp()
        {
            SendRequestTokenWithValidOptions();

            var timeStamp = Now.ToUnixTime().ToString();
            Assert.Equal(timeStamp, CurrentRequest.PostParameters["timestamp"]);
        }

        [Fact]
        public void RequestToken_ShouldPostRandomNonce()
        {
            var currentNonce = "";
            for (int i = 0; i < 10; i++)
            {
                SendRequestTokenWithValidOptions();

                Assert.NotEqual(currentNonce, CurrentRequest.PostParameters["nonce"]);
                currentNonce = CurrentRequest.PostParameters["nonce"];
            }
        }

        [Fact]
        public void RequestToken_ShouldCalculateHMacOfTheCurrentRequestAndBase64EncodeItAndAddItToCurrentRequest()
        {
            var options = SendRequestTokenWithValidOptions();

            var values = new[] 
            { 
                CurrentRequest.PostParameters.Get("id"), 
                CurrentRequest.PostParameters.Get("expires"),
                CurrentRequest.PostParameters.Get("capability", ""), 
                CurrentRequest.PostParameters.Get("client_id", ""), 
                CurrentRequest.PostParameters.Get("timestamp"),
                CurrentRequest.PostParameters.Get("nonce")
            };
            var signText = string.Join("\n", values) + "\n";

            string mac = signText.ComputeHMacSha256(GetKeyValue());

            Assert.Equal(mac, CurrentRequest.PostParameters["mac"]);
        }

        private RequestTokenOptions SendRequestTokenWithValidOptions()
        {
            var rest = GetRestClient();
            var options = new RequestTokenOptions { Capability = "Blah", ClientId = "ClientId", Expires = TimeSpan.FromMinutes(10) };

            //Act
            rest.RequestToken(options);
            return options;
        }
    }
}
