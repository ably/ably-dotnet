using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using IO.Ably.Realtime;
using IO.Ably.Transport;
using IO.Ably.Types;

using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Trait("spec", "RTN15")]
    public class ConnectionFailuresOnceConnectedSpecs : AblyRealtimeSpecs
    {
        private const int TokenErrorCode = 40140;
        private const int FailedRenewalErrorCode = 1234;

        private readonly TokenDetails _returnedDummyTokenDetails = new TokenDetails("123") { Expires = TestHelpers.Now().AddDays(1), ClientId = "123" };
        private readonly TokenDetails _validToken;
        private readonly ErrorInfo _tokenErrorInfo;

        private bool _renewTokenCalled;

        public ConnectionFailuresOnceConnectedSpecs(ITestOutputHelper output)
            : base(output)
        {
            SetNowFunc(() => DateTimeOffset.UtcNow);
            _validToken = new TokenDetails("id") { Expires = Now.AddHours(1) };
            _renewTokenCalled = false;
            _tokenErrorInfo = new ErrorInfo { Code = TokenErrorCode, StatusCode = HttpStatusCode.Unauthorized };
        }

        [Fact]
        [Trait("spec", "RTN15h")]
        public async Task WithDisconnectMessageWithTokenError_ShouldRenewTokenAndReconnect()
        {
            var client = await SetupConnectedClient();

            List<ConnectionState> states = new List<ConnectionState>();
            var errors = new ConcurrentBag<ErrorInfo>();
            client.Connection.On((args) =>
            {
                if (args.HasError)
                {
                    errors.Add(args.Reason);
                }

                states.Add(args.Current);
            });

            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected) { Error = _tokenErrorInfo });

            await client.ProcessCommands();

            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

            await client.WaitForState(ConnectionState.Connected);

            _renewTokenCalled.Should().BeTrue();

            Assert.Equal(new[] { ConnectionState.Disconnected, ConnectionState.Connecting, ConnectionState.Connected }, states);

            errors.Should().HaveCount(1);
            errors.First().Should().Be(_tokenErrorInfo);

            var currentToken = client.RestClient.AblyAuth.CurrentToken;
            currentToken.Token.Should().Be(_returnedDummyTokenDetails.Token);
            currentToken.ClientId.Should().Be(_returnedDummyTokenDetails.ClientId);
            currentToken.Expires.Should().BeCloseTo(_returnedDummyTokenDetails.Expires);
        }

        [Fact]
        [Trait("spec", "RTN15a")]
        public async Task WithDisconnectMessageWithTokenError_ShouldResumeConnection()
        {
            var client = await SetupConnectedClient();

            List<ConnectionState> states = new List<ConnectionState>();
            var errors = new List<ErrorInfo>();
            client.Connection.On((args) =>
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
            });

            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected) { Error = _tokenErrorInfo });

            await client.ProcessCommands();

            var urlParams = LastCreatedTransport.Parameters.GetParams();
            urlParams.Should().ContainKey("resume");
            urlParams.Should().ContainKey("connection_serial");
        }

        [Fact]
        [Trait("spec", "RTN15h2")]
        public async Task WithTokenErrorWhenTokenRenewalFails_ShouldGoToDisconnectedAndEmitError()
        {
            var client = await SetupConnectedClient(true);

            List<ConnectionState> states = new List<ConnectionState>();
            var errors = new List<ErrorInfo>();
            client.Connection.On((args) =>
            {
                if (args.HasError)
                {
                    errors.Add(args.Reason);
                }

                states.Add(args.Current);
            });

            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected)
            {
                Error = _tokenErrorInfo,
            });

            await client.ProcessCommands();

            Assert.Equal(
                new[]
            {
                ConnectionState.Disconnected,
                ConnectionState.Connecting,
                ConnectionState.Disconnected,
            }, states);

            errors.Should().NotBeEmpty();
            errors.Should().HaveCount(2);
            errors[0].Code.Should().Be(40140);
            errors[1].Code.Should().Be(FailedRenewalErrorCode);
        }

        [Fact]
        [Trait("spec", "RTN15h")]
        public async Task WhenConnectionFailsWithTokenErrorButTokenIsNotRenewable_ShouldTransitionDirectlyToFailedWithError()
        {
            var client = await SetupConnectedClient(renewable: false);

            ConcurrentBag<ConnectionState> states = new ConcurrentBag<ConnectionState>();
            var errors = new List<ErrorInfo>();
            client.Connection.On((args) =>
            {
                if (args.HasError)
                {
                    errors.Add(args.Reason);
                }
            });

            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected)
            {
                Error = _tokenErrorInfo
            });

            await client.WaitForState(ConnectionState.Failed);

            errors.Should().NotBeEmpty();
        }

        [Fact]
        [Trait("spec", "RTN15a")]
        [Trait("spec", "RTN15b")]
        [Trait("spec", "RTN15b1")]
        [Trait("spec", "RTN15b2")]
        public async Task WhenTransportCloses_ShouldResumeConnection()
        {
            var client = await SetupConnectedClient();

            List<ConnectionState> states = new List<ConnectionState>();
            var errors = new List<ErrorInfo>();
            client.Connection.On((args) =>
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
            });

            var firstTransport = LastCreatedTransport;
            var connectionKey = client.Connection.Key;
            var serial = client.Connection.Serial.Value;
            LastCreatedTransport.Listener.OnTransportEvent(LastCreatedTransport.Id, TransportState.Closed);

            await client.WaitForState(ConnectionState.Connecting);
            await client.ProcessCommands();

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
            var client = await SetupConnectedClient();

            List<bool> callbackResults = new List<bool>();
            Action<bool, ErrorInfo> callback = (b, info) =>
            {
                callbackResults.Add(b);
            };

            client.ConnectionManager.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Message), callback);
            client.ConnectionManager.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Message), callback);

            await client.ProcessCommands();

            client.State.WaitingForAck.Should().HaveCount(2);

            await CloseAndWaitToReconnect(client);

            LastCreatedTransport.SentMessages.Should().BeEmpty();
            client.State.WaitingForAck.Should().BeEmpty();

            callbackResults.Should().HaveCount(2);
            callbackResults.All(x => x == false).Should().BeTrue();
        }

        [Fact]
        [Trait("spec", "RTN15f")]
        public async Task AckMessagesAreResentWhenConnectionIsDroppedAndResumed()
        {
            var client = await SetupConnectedClient();

            string initialConnectionId = client.Connection.Id;
            client.ConnectionManager.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Message));
            client.ConnectionManager.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Message));

            await CloseAndWaitToReconnect(client, new ProtocolMessage(ProtocolMessage.MessageAction.Connected)
            {
                ConnectionId = initialConnectionId // if the connection ids match then the connection has been resumed
            });

            LastCreatedTransport.SentMessages.Should().HaveCount(2);
            client.State.WaitingForAck.Should().HaveCount(2);
        }

        private Task<AblyRealtime> SetupConnectedClient(bool failRenewal = false, bool renewable = true)
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
                            throw new AblyException(new ErrorInfo("Failed to renew token", FailedRenewalErrorCode));
                        }

                        _renewTokenCalled = true;
                        return _returnedDummyTokenDetails.ToJson().ToAblyResponse();
                    }

                    return AblyResponse.EmptyResponse.ToTask();
                });
        }

        private async Task CloseAndWaitToReconnect(AblyRealtime client, ProtocolMessage connectedMessage = null)
        {
            connectedMessage = connectedMessage ?? new ProtocolMessage(ProtocolMessage.MessageAction.Connected);
            LastCreatedTransport.Listener.OnTransportEvent(LastCreatedTransport.Id, TransportState.Closed);
            await client.WaitForState(ConnectionState.Connecting);
            client.FakeProtocolMessageReceived(connectedMessage);
            await client.WaitForState(ConnectionState.Connected);
            await client.ProcessCommands();
        }
    }
}
