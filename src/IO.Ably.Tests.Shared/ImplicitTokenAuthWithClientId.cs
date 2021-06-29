using System;
using FluentAssertions;
using Xunit;

namespace IO.Ably.Tests
{
    public class ImplicitTokenAuthWithClientId
    {
        private const string ApiKey = "123.456:789";

        internal AblyRequest CurrentRequest { get; set; }

        public AblyRest Client { get; set; }

        public int ExecutionCount { get; set; }

        public int TokenRequestCount { get; set; }

        public ImplicitTokenAuthWithClientId()
        {
            var clientId = "123";
            Client = new AblyRest(new ClientOptions { Key = ApiKey, ClientId = clientId, UseBinaryProtocol = false });
            Client.ExecuteHttpRequest = request =>
            {
                ExecutionCount++;
                if (request.Url.Contains("requestToken"))
                {
                    TokenRequestCount++;
                    long expirationTime = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeInMilliseconds();
                    return $"{{ \"access_token\": {{ \"id\": \"unique-token-id\", \"expires\": \"{expirationTime}\"}}}}".ToAblyResponse();
                }

                return "{}".ToAblyResponse();
            };
        }

        [Fact]
        public void WhenPublishing_WillSendATokenRequestToServer()
        {
            Client.Channels.Get("test").PublishAsync("test", "true");

            ExecutionCount.Should().Be(1);
            TokenRequestCount.Should().Be(0);
        }

        [Fact]
        public void BeforeSendingAMessage_CurrentTokenIsNull()
        {
            Client.AblyAuth.CurrentToken.Should().BeNull();
        }

        [Fact]
        public void AfterSendingAMessage_CurrentTokenHasDefaultCapabilityAndTtl()
        {
            Client.Channels.Get("test").PublishAsync("test", "true");

            Client.AblyAuth.CurrentToken.Should().BeNull();
        }
    }
}
