using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Auth;
using IO.Ably.Realtime;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Trait("spec", "RTN15")]
    public class ConnectionFailuresOnceConnectedSpecs : ConnectionSpecsBase
    {
        private TokenDetails _returnedDummyTokenDetails = new TokenDetails("123") { Expires = Config.Now().AddDays(1), ClientId = "123" };
        private int _tokenErrorCode = 40140;
        private bool _renewTokenCalled;
        private TokenDetails _validToken;

        public AblyRealtime SetupConnectedClient()
        {
            return GetConnectedClient(opts =>
            {
                opts.TokenDetails = _validToken;
                opts.UseBinaryProtocol = false;
            }, request =>
            {
                if (request.Url.Contains("/keys"))
                {
                    _renewTokenCalled = true;
                    return _returnedDummyTokenDetails.ToJson().ToAblyResponse();
                }

                return AblyResponse.EmptyResponse.ToTask();
            });
        }

        [Fact]
        public async Task WithDisconnectMessageWithTokenError_ShouldRenewTokenAndReconnect()
        {
            var client = SetupConnectedClient();

            List<ConnectionStateType> states = new List<ConnectionStateType>();
            
            client.Connection.ConnectionStateChanged += (sender, args) =>
            {
                states.Add(args.CurrentState);
                if (args.CurrentState == ConnectionStateType.Connecting)
                {
                    client.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));
                }
            };
            await client.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected) { error = new ErrorInfo() { code = _tokenErrorCode, statusCode = HttpStatusCode.Unauthorized } });

            _renewTokenCalled.Should().BeTrue();
            Assert.Equal(new [] { ConnectionStateType.Disconnected, ConnectionStateType.Connecting, ConnectionStateType.Connected }, states);

            var currentToken = client.Auth.CurrentToken;
            currentToken.Token.Should().Be(_returnedDummyTokenDetails.Token);
            currentToken.ClientId.Should().Be(_returnedDummyTokenDetails.ClientId);
            currentToken.Expires.Should().BeCloseTo(_returnedDummyTokenDetails.Expires);
        }

        public ConnectionFailuresOnceConnectedSpecs(ITestOutputHelper output) : base(output)
        {
            Now = DateTimeOffset.Now;
            _validToken = new TokenDetails("id") { Expires = Now.AddHours(1) };
            _renewTokenCalled = false;
        }
    }
}