using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Auth;
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
        private TokenDetails _returnedDummyTokenDetails = new TokenDetails("123") { Expires = Config.Now().AddDays(1), ClientId = "123" };
        private int _tokenErrorCode = 40140;
        private bool _renewTokenCalled;
        private TokenDetails _validToken;
        private ErrorInfo _tokenErrorInfo;
        private int _failedRenewalErorrCode = 1234;

        public AblyRealtime SetupConnectedClient(bool failRenewal = false, bool renewable = true)
        {
            return GetConnectedClient(opts =>
            {
                if (renewable == false) opts.Key = ""; //clear the key to make the token non renewable
                opts.TokenDetails = _validToken;
                opts.UseBinaryProtocol = false;
            }, request =>
            {
                if (request.Url.Contains("/keys"))
                {
                    if (failRenewal)
                        throw new AblyException(new ErrorInfo("Failed to renew token", _failedRenewalErorrCode));
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

            List<ConnectionStateType> states = new List<ConnectionStateType>();
            var errors = new List<ErrorInfo>();
            client.Connection.ConnectionStateChanged += (sender, args) => 
            {
                if (args.HasError)
                    errors.Add(args.Reason);

                states.Add(args.CurrentState);
            };

            await client.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected) { error = _tokenErrorInfo });
            await client.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

            _renewTokenCalled.Should().BeTrue();
            Assert.Equal(new[] { ConnectionStateType.Disconnected, ConnectionStateType.Connecting, ConnectionStateType.Connected }, states);
            errors.Should().BeEmpty("There should be no errors emitted by the client");

            var currentToken = client.Auth.CurrentToken;
            currentToken.Token.Should().Be(_returnedDummyTokenDetails.Token);
            currentToken.ClientId.Should().Be(_returnedDummyTokenDetails.ClientId);
            currentToken.Expires.Should().BeCloseTo(_returnedDummyTokenDetails.Expires);
        }

        [Fact]
        [Trait("spec", "RTN15a")]
        public async Task WithDisconnectMessageWithTokenError_ShouldResumeConnection()
        {
            var client = SetupConnectedClient();

            List<ConnectionStateType> states = new List<ConnectionStateType>();
            var errors = new List<ErrorInfo>();
            client.Connection.ConnectionStateChanged += (sender, args) =>
            {
                if (args.HasError)
                    errors.Add(args.Reason);

                states.Add(args.CurrentState);
                if (args.CurrentState == ConnectionStateType.Connecting)
                {
                    client.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));
                }
            };

            await client.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected) { error = _tokenErrorInfo });

            var urlParams = LastCreatedTransport.Parameters.GetParams();
            urlParams.Should().ContainKey("resume");
            urlParams.Should().ContainKey("connection_serial");
        }

        [Fact]
        [Trait("spec", "RTN15h")]
        public async Task WithTokenErrorWhenTokenRenewalFails_ShouldGoToFailedStateAndEmitError()
        {
            var client = SetupConnectedClient(failRenewal: true);

            List<ConnectionStateType> states = new List<ConnectionStateType>();
            var errors = new List<ErrorInfo>();
            client.Connection.ConnectionStateChanged += (sender, args) =>
            {
                if (args.HasError)
                    errors.Add(args.Reason);

                states.Add(args.CurrentState);
            };

            await client.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected)
                {
                    error = _tokenErrorInfo
                });

            Assert.Equal(new[]
            {
                ConnectionStateType.Disconnected,
                ConnectionStateType.Connecting,
                ConnectionStateType.Failed
            }, states);

            errors.Should().NotBeEmpty();
            errors.First().code.Should().Be(_failedRenewalErorrCode);
        }

        [Fact]
        [Trait("spec", "RTN15h")]
        public async Task WhenConnectionFailsConsecutivelyMoreThanOnceWithTokenError_ShouldTransitionToFailedWithError()
        {
            var client = SetupConnectedClient();

            List<ConnectionStateType> states = new List<ConnectionStateType>();
            var errors = new List<ErrorInfo>();
            client.Connection.ConnectionStateChanged += (sender, args) =>
            {
                if (args.HasError)
                    errors.Add(args.Reason);

                states.Add(args.CurrentState);
            };

            await client.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected)
            {
                error = _tokenErrorInfo
            });

            await client.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
            {
                error = _tokenErrorInfo
            });

            Assert.Equal(new[]
            {
                ConnectionStateType.Disconnected,
                ConnectionStateType.Connecting,
                ConnectionStateType.Failed
            }, states);

            errors.Should().NotBeEmpty();
            errors.First().code.Should().Be(_tokenErrorInfo.code);
        }

        [Fact]
        [Trait("spec", "RTN15h")]
        public async Task WhenConnectionFailsWithTokenErrorButTokenIsNotRenewable_ShouldTransitionDirectlyToFailedWithError()
        {
            var client = SetupConnectedClient(renewable: false);

            List<ConnectionStateType> states = new List<ConnectionStateType>();
            var errors = new List<ErrorInfo>();
            client.Connection.ConnectionStateChanged += (sender, args) =>
            {
                if (args.HasError)
                    errors.Add(args.Reason);

                states.Add(args.CurrentState);
            };

            await client.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected)
            {
                error = _tokenErrorInfo
            });

            Assert.Equal(new[]
            {
                ConnectionStateType.Failed
            }, states);

            errors.Should().NotBeEmpty();
        }

        [Fact]
        [Trait("spec", "RTN15a")]
        [Trait("spec", "RTN15b")]
        [Trait("spec", "RTN15b1")]
        [Trait("spec", "RTN15b2")]
        public async Task WhenTransportCloses_ShouldResumeConnection()
        {
            var client = SetupConnectedClient();

            List<ConnectionStateType> states = new List<ConnectionStateType>();
            var errors = new List<ErrorInfo>();
            client.Connection.ConnectionStateChanged += (sender, args) =>
            {
                if (args.HasError)
                    errors.Add(args.Reason);

                states.Add(args.CurrentState);
                if (args.CurrentState == ConnectionStateType.Connecting)
                {
                    client.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));
                }
            };

            var firstTransport = LastCreatedTransport;
            var connectionKey = client.Connection.Key;
            var serial = client.Connection.Serial.Value;
            LastCreatedTransport.Listener.OnTransportEvent(TransportState.Closed);

            await new ConnectionAwaiter(client.Connection, ConnectionStateType.Connecting).Wait(TimeSpan.FromSeconds(5));

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
            await new ConnectionAwaiter(client.Connection, ConnectionStateType.Connecting).Wait();
            await client.FakeMessageReceived(protocolMessage);
        }

        [Fact]
        [Trait("spec", "RTN15f")]
        public async Task AckMessagesAreResentWhenConnectionIsDroppedAndResumed()
        {
            var client = SetupConnectedClient();
            client.Connection.ConnectionStateChanged += (sender, args) =>
            {
                if (args.CurrentState == ConnectionStateType.Connecting)
                {
                    client.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));
                }
            };
            List<bool> callbackResults = new List<bool>();
            Action<bool, ErrorInfo> callback = (b, info) =>
            {
                callbackResults.Add(b);
            };

            string initialConnectionId = client.Connection.Id;
            client.ConnectionManager.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Message), callback);
            client.ConnectionManager.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Message), callback);

            await CloseAndWaitToReconnect(client, new ProtocolMessage(ProtocolMessage.MessageAction.Connected)
            {
                connectionId = initialConnectionId // if the connection ids match then the connection has been resumed
            });

            LastCreatedTransport.SentMessages.Should().HaveCount(2);
            client.ConnectionManager.AckProcessor.GetQueuedMessages().Should().HaveCount(2);
        }



        public ConnectionFailuresOnceConnectedSpecs(ITestOutputHelper output) : base(output)
        {
            Now = DateTimeOffset.Now;
            _validToken = new TokenDetails("id") { Expires = Now.AddHours(1) };
            _renewTokenCalled = false;
            _tokenErrorInfo = new ErrorInfo() { code = _tokenErrorCode, statusCode = HttpStatusCode.Unauthorized };
        }
    }
}