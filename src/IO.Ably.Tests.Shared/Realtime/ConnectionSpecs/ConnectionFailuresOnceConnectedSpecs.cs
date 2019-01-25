using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Transport;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Trait("spec", "RTN15")]
    public class ConnectionFailuresOnceConnectedSpecs : ConnectionSpecsBase
    {
        private TokenDetails _returnedDummyTokenDetails = new TokenDetails("123") { Expires = TestHelpers.Now().AddDays(1), ClientId = "123" };
        private int _tokenErrorCode = 40140;
        private bool _renewTokenCalled;
        private TokenDetails _validToken;
        private ErrorInfo _tokenErrorInfo;
        private int _failedRenewalErorrCode = 1234;

        public AblyRealtime SetupConnectedClient(bool failRenewal = false, bool renewable = true)
        {
            return GetConnectedClient(
                opts =>
            {
                if (renewable == false)
                {
                    opts.Key = string.Empty; // clear the key to make the token non renewable
                }

                opts.TokenDetails = _validToken;
                opts.UseBinaryProtocol = false;
            }, request =>
            {
                if (request.Url.Contains("/keys"))
                {
                    if (failRenewal)
                    {
                        throw new AblyException(new ErrorInfo("Failed to renew token", _failedRenewalErorrCode));
                    }

                    _renewTokenCalled = true;
                    return _returnedDummyTokenDetails.ToJson().ToAblyResponse();
                }

                return AblyResponse.EmptyResponse.ToTask();
            });
        }

        [Fact]
        [Trait("spec", "RTN15h")]
        public async Task WithDisconnectMessageWithTokenError_ShouldRenewTokenAndReconnect()
        {
            var client = SetupConnectedClient();

            List<ConnectionState> states = new List<ConnectionState>();
            var errors = new List<ErrorInfo>();
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                if (args.HasError)
                {
                    errors.Add(args.Reason);
                }

                states.Add(args.Current);
            };

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected) { Error = _tokenErrorInfo });
            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

            _renewTokenCalled.Should().BeTrue();
            Assert.Equal(new[] { ConnectionState.Disconnected, ConnectionState.Connecting, ConnectionState.Connected }, states);
            errors.Should().HaveCount(1);
            errors[0].Should().Be(_tokenErrorInfo);

            var currentToken = client.RestClient.AblyAuth.CurrentToken;
            currentToken.Token.Should().Be(_returnedDummyTokenDetails.Token);
            currentToken.ClientId.Should().Be(_returnedDummyTokenDetails.ClientId);
            currentToken.Expires.Should().BeCloseTo(_returnedDummyTokenDetails.Expires);
        }

        [Fact]
        [Trait("spec", "RTN15a")]
        public async Task WithDisconnectMessageWithTokenError_ShouldResumeConnection()
        {
            var client = SetupConnectedClient();

            List<ConnectionState> states = new List<ConnectionState>();
            var errors = new List<ErrorInfo>();
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                if (args.HasError)
                {
                    errors.Add(args.Reason);
                }

                states.Add(args.Current);
                if (args.Current == ConnectionState.Connecting)
                {
                    client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));
                }
            };

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected) { Error = _tokenErrorInfo });

            var urlParams = LastCreatedTransport.Parameters.GetParams();
            urlParams.Should().ContainKey("resume");
            urlParams.Should().ContainKey("connection_serial");
        }

        [Fact]
        [Trait("spec", "RTN15h")]
        public async Task WithTokenErrorWhenTokenRenewalFails_ShouldGoToFailedStateAndEmitError()
        {
            var client = SetupConnectedClient(true);

            List<ConnectionState> states = new List<ConnectionState>();
            var errors = new List<ErrorInfo>();
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                if (args.HasError)
                {
                    errors.Add(args.Reason);
                }

                states.Add(args.Current);
            };

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected)
                {
                    Error = _tokenErrorInfo
                });

            Assert.Equal(
                new[]
            {
                ConnectionState.Disconnected,
                ConnectionState.Connecting,
                ConnectionState.Failed
            }, states);

            errors.Should().NotBeEmpty();
            errors.Should().HaveCount(2);
            errors[1].Code.Should().Be(_failedRenewalErorrCode);
        }

        [Fact]
        [Trait("spec", "RTN15h")]
        public async Task WhenConnectionFailsWithTokenErrorButTokenIsNotRenewable_ShouldTransitionDirectlyToFailedWithError()
        {
            var client = SetupConnectedClient(renewable: false);

            List<ConnectionState> states = new List<ConnectionState>();
            var errors = new List<ErrorInfo>();
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                if (args.HasError)
                {
                    errors.Add(args.Reason);
                }

                states.Add(args.Current);
            };

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected)
            {
                Error = _tokenErrorInfo
            });

            Assert.Equal(
                new[]
            {
                ConnectionState.Failed
            }, states);

            errors.Should().NotBeEmpty();
        }

        [Fact]
        [Trait("spec", "RTN15a")]
        [Trait("spec", "RTN15b")]
        [Trait("spec", "RTN15b1")]
        [Trait("spec", "RTN15b2")]
        public void WhenTransportCloses_ShouldResumeConnection()
        {
            var client = SetupConnectedClient();

            List<ConnectionState> states = new List<ConnectionState>();
            var errors = new List<ErrorInfo>();
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                if (args.HasError)
                {
                    errors.Add(args.Reason);
                }

                states.Add(args.Current);
                if (args.Current == ConnectionState.Connecting)
                {
                    client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));
                }
            };

            var firstTransport = LastCreatedTransport;
            var connectionKey = client.Connection.Key;
            var serial = client.Connection.Serial.Value;
            LastCreatedTransport.Listener.OnTransportEvent(TransportState.Closed);

            var urlParams = LastCreatedTransport.Parameters.GetParams();
            urlParams.Should().ContainKey("resume")
                .WhichValue.Should().Be(connectionKey);
            urlParams.Should().ContainKey("connection_serial")
                .WhichValue.Should().Be(serial.ToString());
            LastCreatedTransport.Should().NotBeSameAs(firstTransport);
        }

        [Fact]
        [Trait("spec", "RTN15f")]
        public async Task AckMessagesAreFailedWhenConnectionIsDroppedAndNotResumed()
        {
            var client = SetupConnectedClient();

            List<bool> callbackResults = new List<bool>();
            Action<bool, ErrorInfo> callback = (b, info) =>
            {
                callbackResults.Add(b);
            };

            client.ConnectionManager.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Message), callback);
            client.ConnectionManager.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Message), callback);
            client.ConnectionManager.AckProcessor.GetQueuedMessages().Should().HaveCount(2);

            await CloseAndWaitToReconnect(client);

            LastCreatedTransport.SentMessages.Should().BeEmpty();
            client.ConnectionManager.AckProcessor.GetQueuedMessages().Should().BeEmpty();

            callbackResults.Should().HaveCount(2);
            callbackResults.All(x => x == false).Should().BeTrue();
        }

        private async Task CloseAndWaitToReconnect(AblyRealtime client, ProtocolMessage protocolMessage = null)
        {
            protocolMessage = protocolMessage ?? new ProtocolMessage(ProtocolMessage.MessageAction.Connected);
            LastCreatedTransport.Listener.OnTransportEvent(TransportState.Closed);
            await new ConnectionAwaiter(client.Connection, ConnectionState.Connecting).Wait();
            await client.FakeProtocolMessageReceived(protocolMessage);
        }

        [Fact]
        [Trait("spec", "RTN15f")]
        public async Task AckMessagesAreResentWhenConnectionIsDroppedAndResumed()
        {
            var client = SetupConnectedClient();

            string initialConnectionId = client.Connection.Id;
            client.ConnectionManager.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Message));
            client.ConnectionManager.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Message));

            await CloseAndWaitToReconnect(client, new ProtocolMessage(ProtocolMessage.MessageAction.Connected)
            {
                ConnectionId = initialConnectionId // if the connection ids match then the connection has been resumed
            });

            LastCreatedTransport.SentMessages.Should().HaveCount(2);
            client.ConnectionManager.AckProcessor.GetQueuedMessages().Should().HaveCount(2);
        }

        public ConnectionFailuresOnceConnectedSpecs(ITestOutputHelper output)
            : base(output)
        {
            SetNowFunc(() => DateTimeOffset.UtcNow);
            _validToken = new TokenDetails("id") { Expires = Now.AddHours(1) };
            _renewTokenCalled = false;
            _tokenErrorInfo = new ErrorInfo() { Code = _tokenErrorCode, StatusCode = HttpStatusCode.Unauthorized };
        }
    }
}
