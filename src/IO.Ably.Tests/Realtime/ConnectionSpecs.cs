using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Auth;
using IO.Ably.Realtime;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    public class ConnectionSpecs : AblyRealtimeSpecs
    {
        private FakeTransportFactory _fakeTransportFactory;
        protected FakeTransport LastCreatedTransport => _fakeTransportFactory.LastCreatedTransport;

        protected void FakeMessageReceived(ProtocolMessage message)
        {
            LastCreatedTransport.Listener.OnTransportMessageReceived(message);
        }

        internal AblyRealtime GetClientWithFakeTransport(Action<ClientOptions> optionsAction = null, Func<AblyRequest, Task<AblyResponse>> handleRequestFunc = null)
        {
            var options = new ClientOptions(ValidKey) { TransportFactory = _fakeTransportFactory };
            optionsAction?.Invoke(options);
            var client = GetRealtimeClient(options, handleRequestFunc);
            return client;
        }

        internal AblyRealtime GetConnectedClient(Action<ClientOptions> optionsAction = null, Func<AblyRequest, Task<AblyResponse>> handleRequestFunc = null)
        {
            var client = GetClientWithFakeTransport(optionsAction, handleRequestFunc);
            FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));
            return client;
        }

        public ConnectionSpecs(ITestOutputHelper output) : base(output)
        {
            _fakeTransportFactory = new FakeTransportFactory();
        }

        public class GeneralSpecs : ConnectionSpecs
        {
            [Fact]
            [Trait("spec", "RTN1")]
            public void ShouldUseWebSocketTransport()
            {
                var client = GetRealtimeClient();

                client.ConnectionManager.Transport.GetType().Should().Be(typeof(WebSocketTransport));
            }

            [Fact]
            [Trait("spec", "RTN3")]
            [Trait("spec", "RTN6")]
            public void WithAutoConnect_CallsConnectOnTransport()
            {
                var client = GetClientWithFakeTransport(opts => opts.AutoConnect = true);
                FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

                client.ConnectionManager.ConnectionState.Should().Be(ConnectionStateType.Connected);
                LastCreatedTransport.ConnectCalled.Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RTN3")]
            public void WithAutoConnectFalse_LeavesStateAsInitialized()
            {
                var client = GetClientWithFakeTransport(opts => opts.AutoConnect = false);

                client.ConnectionManager.ConnectionState.Should().Be(ConnectionStateType.Initialized);
                LastCreatedTransport.Should().BeNull("Transport shouldn't be created without calling connect when AutoConnect is false");
            }

            [Fact]
            [Trait("spec", "RTN19")]
            public void WhenConnectedMessageReceived_ConnectionShouldBeInConnectedStateAndConnectionDetailsAreUpdated()
            {
                var client = GetClientWithFakeTransport();

                var connectionDetailsMessage = new ConnectionDetailsMessage()
                {
                    clientId = "123",
                    connectionKey = "boo"
                };
                FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected)
                {
                    connectionDetails = connectionDetailsMessage,
                    connectionKey = "unimportant"
                });

                client.Connection.State.Should().Be(ConnectionStateType.Connected);
                client.Connection.Key.Should().Be("boo");
            }

            [Fact]
            [Trait("spec", "RTN19")]
            public void WhenConnectedMessageReceived_WithNoConnectionDetailsButConnectionKeyInMessage_ShouldHaveCorrectKey()
            {
                var client = GetClientWithFakeTransport();

                FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected)
                {
                    connectionKey = "unimportant"
                });

                client.Connection.Key.Should().Be("unimportant");
            }

            [Fact]
            [Trait("spec", "RSA15a")]
            [Trait("sandboxTest", "needed")]
            public void WhenConnectedMessageReceivedWithClientId_AblyAuthShouldUseConnectionClientId()
            {
                var client = GetClientWithFakeTransport();

                FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected)
                {
                    connectionDetails = new ConnectionDetailsMessage { clientId = "realtimeClient" }
                });

                client.RestClient.AblyAuth.GetClientId().Should().Be("realtimeClient");
            }

            public GeneralSpecs(ITestOutputHelper output) : base(output)
            {
            }
        }

        [Trait("spec", "RTN2")]
        public class ConnectionParameterSpecs : ConnectionSpecs
        {
            [Fact]
            [Trait("spec", "RTN2")]
            public void ShouldUseDefaultRealtimeHost()
            {
                var client = GetClientWithFakeTransport();
                LastCreatedTransport.Parameters.Host.Should().Be(Defaults.RealtimeHost);
            }

            [Theory]
            [InlineData(true, "msgpack")]
            [InlineData(false, "json")]
            [Trait("spec", "RTN2a")]
            public void WithUseBinaryEncoding_ShouldSetTransportFormatProperty(bool useBinary, string format)
            {
                var client = GetClientWithFakeTransport(opts => opts.UseBinaryProtocol = useBinary);
                LastCreatedTransport.Parameters.UseBinaryProtocol.Should().Be(useBinary);
                LastCreatedTransport.Parameters.GetParams().Should().ContainKey("format").WhichValue.Should().Be(format);
            }

            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            [Trait("spec", "RTN2b")]
            public void WithEchoInClientOptions_ShouldSetTransportEchoCorrectly(bool echo)
            {
                var client = GetClientWithFakeTransport(opts => opts.EchoMessages = echo);

                LastCreatedTransport.Parameters.EchoMessages.Should().Be(echo);
                LastCreatedTransport.Parameters.GetParams()
                    .Should().ContainKey("echo")
                    .WhichValue.Should().Be(echo.ToString().ToLower());
            }

            [Fact]
            [Trait("spec", "RTN2d")]
            public void WithClientId_ShouldSetTransportClientIdCorrectly()
            {
                var clientId = "12345";
                var client = GetClientWithFakeTransport(opts =>
                {
                    opts.ClientId = clientId;
                    opts.Token = "123";

                });

                LastCreatedTransport.Parameters.ClientId.Should().Be(clientId);
                LastCreatedTransport.Parameters.GetParams()
                    .Should().ContainKey("clientId")
                    .WhichValue.Should().Be(clientId);
            }

            [Fact]
            [Trait("spec", "RTN2d")]
            public void WithoutClientId_ShouldNotSetClientIdParameterOnTransport()
            {
                var client = GetClientWithFakeTransport();

                LastCreatedTransport.Parameters.ClientId.Should().BeNullOrEmpty();
                LastCreatedTransport.Parameters.GetParams().Should().NotContainKey("clientId");
            }

            [Fact]
            [Trait("spec", "RTN2e")]
            public void WithBasicAuth_ShouldSetTransportKeyParameter()
            {
                var client = GetClientWithFakeTransport();
                LastCreatedTransport.Parameters.AuthValue.Should().Be(client.Options.Key);
                LastCreatedTransport.Parameters.GetParams().
                    Should().ContainKey("key")
                    .WhichValue.Should().Be(client.Options.Key);
            }

            [Fact]
            [Trait("spec", "RTN2e")]
            public void WithTokenAuth_ShouldSetTransportAccessTokeParameter()
            {
                var clientId = "123";
                var tokenString = "token";
                var client = GetClientWithFakeTransport(opts =>
                {
                    opts.Key = "";
                    opts.ClientId = clientId;
                    opts.Token = tokenString;

                });

                LastCreatedTransport.Parameters.AuthValue.Should().Be(tokenString);
                LastCreatedTransport.Parameters.GetParams()
                    .Should().ContainKey("accessToken")
                    .WhichValue.Should().Be(tokenString);
            }

            [Fact]
            [Trait("spec", "RTN2f")]
            public void ShouldSetTransportVersionParameterTov08()
            {
                var client = GetClientWithFakeTransport();

                LastCreatedTransport.Parameters.GetParams()
                    .Should().ContainKey("v")
                    .WhichValue.Should().Be("0.8");
            }

            public ConnectionParameterSpecs(ITestOutputHelper output) : base(output)
            {

            }
        }

        [Trait("spec", "RTN4")]
        public class EventEmitterSpecs : ConnectionSpecs
        {
            [Fact]
            [Trait("spec", "RTN4a")]
            public void EmittedEventTypesShouldBe()
            {
                var states = Enum.GetNames(typeof(ConnectionStateType));
                states.ShouldBeEquivalentTo(new[]
                {
                    "Initialized",
                    "Connecting",
                    "Connected",
                    "Disconnected",
                    "Suspended",
                    "Closing",
                    "Closed",
                    "Failed"
                });
            }

            [Fact]
            [Trait("spec", "RTN4b")]
            [Trait("spec", "RTN4d")]
            [Trait("spec", "RTN4e")]
            public void ANewConnectionShouldRaiseConnectingAndConnectedEvents()
            {
                var client = GetClientWithFakeTransport(opts => opts.AutoConnect = false);
                var states = new List<ConnectionStateType>();
                client.Connection.ConnectionStateChanged += (sender, args) =>
                {
                    args.Should().BeOfType<ConnectionStateChangedEventArgs>();
                    states.Add(args.CurrentState);
                };

                client.Connect();
                //SendConnected Message
                LastCreatedTransport.Listener.OnTransportMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

                states.Should().BeEquivalentTo(new[] { ConnectionStateType.Connecting, ConnectionStateType.Connected });
                client.Connection.State.Should().Be(ConnectionStateType.Connected);
            }

            [Fact]
            [Trait("spec", "RTN4c")]
            [Trait("spec", "RTN4d")]
            [Trait("spec", "RTN4e")]
            public void WhenClosingAConnection_ItShouldRaiseClosingAndClosedEvents()
            {
                var client = GetClientWithFakeTransport();

                //Start collecting events after the connection is open
                var states = new List<ConnectionStateType>();
                client.Connection.ConnectionStateChanged += (sender, args) =>
                {
                    args.Should().BeOfType<ConnectionStateChangedEventArgs>();
                    states.Add(args.CurrentState);
                };
                LastCreatedTransport.SendAction = message =>
                {
                    if (message.action == ProtocolMessage.MessageAction.Close)
                    {
                        LastCreatedTransport.Close();
                    }
                };

                client.Close();
                states.Should().BeEquivalentTo(new[] { ConnectionStateType.Closing, ConnectionStateType.Closed });
                client.Connection.State.Should().Be(ConnectionStateType.Closed);
            }

            [Fact]
            [Trait("spec", "RTN4f")]
            [Trait("sandboxTest", "needed")]
            public async Task WithAConnectionError_ShouldRaiseChangeStateEventWithError()
            {
                var client = GetClientWithFakeTransport();
                bool hasError = false;
                ErrorInfo actualError = null;
                client.Connection.ConnectionStateChanged += (sender, args) =>
                {
                    hasError = args.HasError;
                    actualError = args.Reason;
                };
                var expectedError = new ErrorInfo();

                LastCreatedTransport.Listener.OnTransportMessageReceived(
                    new ProtocolMessage(ProtocolMessage.MessageAction.Error) { error = expectedError });

                hasError.Should().BeTrue();
                actualError.Should().Be(expectedError);
            }

            public EventEmitterSpecs(ITestOutputHelper output) : base(output)
            {
            }
        }

        [Trait("spec", "RTN7")]
        public class AckNackSpecs : ConnectionSpecs
        {
            private AblyRealtime _realtime;
            private FakeAckProcessor _ackProcessor;
            // This only contains the AckProcessor integration with the ConnectionManager. 
            // The Actual Ack processor tests are in AckProtocolSpecs.cs

            [Fact]  
            public void ShouldListenToConnectionStateChanges()
            {
                ((IConnectionContext) _realtime.ConnectionManager).SetState(
                    new ConnectionFailedState(_realtime.ConnectionManager, new ErrorInfo()));

                _ackProcessor.OnStatecChanged.Should().BeTrue();
                _ackProcessor.LastState.Should().BeOfType<ConnectionFailedState>();
            }

            [Fact]
            public void WhenSendIsCalled_ShouldPassTheMessageThroughTHeAckProcessor()
            {
                var message = new ProtocolMessage(ProtocolMessage.MessageAction.Message);

                _realtime.ConnectionManager.Send(message, null);
                _ackProcessor.SendMessageCalled.Should().BeTrue();
            }

            [Fact]
            public void WhemMessageReceived_ShouldPassTheMessageThroughTheAckProcessor()
            {
                var message = new ProtocolMessage(ProtocolMessage.MessageAction.Ack);
                ((ITransportListener) _realtime.ConnectionManager).OnTransportMessageReceived(message);

                _ackProcessor.OnMessageReceivedCalled.Should().BeTrue();
            }

            public AckNackSpecs(ITestOutputHelper output) : base(output)
            {
                _ackProcessor = new FakeAckProcessor();
                _realtime = GetRealtimeClient();
                _realtime.ConnectionManager.AckProcessor = _ackProcessor;
            }
        }

        [Trait("spec", "RTN8")]
        public class ConnectionIdSpecs : ConnectionSpecs
        {
            [Fact]
            [Trait("spec", "RTN8a")]
            public void ConnectionIdIsNull_WhenClientIsNotConnected()
            {
                var client = GetClientWithFakeTransport(opts => opts.AutoConnect = false);
                client.Connection.Id.Should().BeNullOrEmpty();
            }

            [Fact]
            [Trait("spec", "RTN8b")]
            [Trait("sandboxTest", "needed")]
            public void ConnectionIdSetBasedOnValueProvidedByAblyService()
            {
                var client = GetClientWithFakeTransport();
                FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected) { connectionId = "123"});
                client.Connection.Id.Should().Be("123");
            }

            public ConnectionIdSpecs(ITestOutputHelper output) : base(output)
            {
            }
        }

        [Trait("spec", "RTN9")]
        public class ConnectionKeySpecs : ConnectionSpecs
        {
            [Fact]
            [Trait("spec", "RTN9a")]
            public void UntilConnected_ShouldBeNull()
            {
                var client = GetClientWithFakeTransport(opts => opts.AutoConnect = false);
                client.Connection.Key.Should().BeNullOrEmpty();
            }

            [Fact]
            [Trait("spec", "RTN9b")]
            [Trait("sandboxTest", "needed")]
            public void OnceConnected_ShouldUseKeyFromConnectedMessage()
            {
                var client = GetClientWithFakeTransport();
                FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected) { connectionDetails = new ConnectionDetailsMessage() { connectionKey = "key"} });
                client.Connection.Key.Should().Be("key");
            }

            [Fact]
            [Trait("spec", "RTN9b")]
            public async Task WhenRestoringConnection_UsesConnectionKey()
            {
                // Arrange
                string targetKey = "1234567";
                var client = GetClientWithFakeTransport();
                client.Connection.Key = targetKey;

                // Act
                var transportParamsForReconnect = await client.ConnectionManager.CreateTransportParameters();

                // Assert
                transportParamsForReconnect
                    .ConnectionKey.Should().Be(targetKey);
            }

            public ConnectionKeySpecs(ITestOutputHelper output) : base(output)
            {
            }
        }

        [Trait("spec", "RTN10")]
        public class ConnectionSerialSpecs : ConnectionSpecs
        {
            [Fact]
            [Trait("spec", "RTN10a")]
            public void OnceConnected_ConnectionSerialShouldBeMinusOne()
            {
                var client = GetClientWithFakeTransport();
                FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));
                client.Connection.Serial.Should().Be(-1);
            }

            [Fact]
            [Trait("spec", "RTN10c")]
            public async Task WhenRestoringConnection_UsesLastKnownConnectionSerial()
            {
                // Arrange
                var client = GetClientWithFakeTransport();
                long targetSerial = 1234567;
                client.Connection.Serial = targetSerial;

                // Act
                var transportParams =await client.ConnectionManager.CreateTransportParameters();

                transportParams.ConnectionSerial.Should().Be(targetSerial);
            }

            [Fact]
            [Trait("spec", "RTN10b")]
            public void WhenProtocolMessageWithSerialReceived_SerialShouldUpdate()
            {
                // Arrange
                var client = GetClientWithFakeTransport();
                FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

                FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Message)
                {
                    connectionSerial = 123456
                });

                // Act
                client.Connection.Serial.Should().Be(123456);
            }

            [Fact]
            [Trait("spec", "RTN10b")]
            public void WhenProtocolMessageWithOUTSerialReceived_SerialShouldNotUpdate()
            {
                // Arrange
                var client = GetClientWithFakeTransport();
                FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));
                var initialSerial = client.Connection.Serial;

                FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Message));

                // Act
                client.Connection.Serial.Should().Be(initialSerial);
            }

            [Fact(Skip = "Need to get back to it")]
            [Trait("spec", "RTN10b")]
            public void WhenFirstAckMessageReceived_ShouldSetSerialToZero()
            {

            }


            public ConnectionSerialSpecs(ITestOutputHelper output) : base(output)
            {
            }
        }

        [Trait("spec", "RTN13")]
        public class ConnectionPingSpecs : ConnectionSpecs
        {
            [Fact]
            public async Task ShouldSendHeartbeatMessage()
            {
                var client = GetConnectedClient();

                var result = await client.Connection.Ping();

                LastCreatedTransport.LastMessageSend.action.Should().Be(ProtocolMessage.MessageAction.Heartbeat);
            }

            [Fact]
            [Trait("spec", "RTN13a")]
            public async Task OnHeartBeatMessageReceived_ShouldReturnElapsedTime()
            {
                Now = DateTimeOffset.UtcNow;
                var client = GetConnectedClient();

                _fakeTransportFactory.LastCreatedTransport.SendAction = async message =>
                {
                    Now = Now.AddMilliseconds(100);
                    if (message.action == ProtocolMessage.MessageAction.Heartbeat)
                    {
                        await Task.Delay(1);
                        FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));
                    }
                };
                var result = await client.Connection.Ping();

                result.IsSuccess.Should().BeTrue();
                result.Value.Value.Should().Be(TimeSpan.FromMilliseconds(100));
            }

            [Fact]
            [Trait("spec", "RTN13b")]
            public async Task WithClosedOrFailedConnectionStates_ShouldReturnError()
            {
                var client = GetClientWithFakeTransport();

                ((IConnectionContext)client.ConnectionManager).SetState(new ConnectionClosedState(client.ConnectionManager, new ErrorInfo()));

                var result = await client.Connection.Ping();

                result.IsSuccess.Should().BeFalse();
                result.Error.Should().Be(ConnectionHeartbeatRequest.DefaultError);

                ((IConnectionContext)client.ConnectionManager).SetState(new ConnectionFailedState(client.ConnectionManager, new ErrorInfo()));

                var resultFailed = await client.Connection.Ping();

                resultFailed.IsSuccess.Should().BeFalse();
                resultFailed.Error.Should().Be(ConnectionHeartbeatRequest.DefaultError);
            }

            [Fact]
            [Trait("spec", "RTN13c")]
            public async Task WhenDefaultTimeoutExpiresWithoutReceivingHeartbeatMessage_ShouldFailWithTimeoutError()
            {
                var client = GetConnectedClient(opts => opts.RealtimeRequestTimeout = TimeSpan.FromMilliseconds(100));

                var result = await client.Connection.Ping();

                result.IsSuccess.Should().BeFalse();
                result.Error.statusCode.Should().Be(HttpStatusCode.RequestTimeout);
            }

            public ConnectionPingSpecs(ITestOutputHelper output) : base(output)
            {
            }
        }

        public class ConnectionFailureSpecs : ConnectionSpecs
        {
            private TokenDetails _returnedDummyTokenDetails = new TokenDetails("123") { Expires = Config.Now().AddDays(1), ClientId = "123" };
            private int _tokenErrorCode = 40140;

            [Fact]
            [Trait("spec", "RTN14b")]
            public async Task WithTokenErrorAndRenewableToken_ShouldRenewTokenAutomaticallyWithoutEmittingError()
            {
                Now = DateTimeOffset.Now;
                var tokenDetails = new TokenDetails("id") { Expires = Now.AddHours(1) };
                bool renewTokenCalled = false;
                var client = GetClientWithFakeTransport(opts =>
                {
                    opts.TokenDetails = tokenDetails;
                    opts.UseBinaryProtocol = false;
                }, request =>
                {
                    if (request.Url.Contains("/keys"))
                    {
                        renewTokenCalled = true;
                        return _returnedDummyTokenDetails.ToJson().ToAblyResponse();
                    }

                    return AblyResponse.EmptyResponse.ToTask();
                });

                List<ErrorInfo> raisedErrors = new List<ErrorInfo>();
                client.Connection.ConnectionStateChanged += (sender, args) =>
                {
                     if(args.HasError)
                        raisedErrors.Add(args.Reason);
                };

                FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { error = new ErrorInfo("Unauthorised", _tokenErrorCode, HttpStatusCode.Unauthorized) });

                renewTokenCalled.Should().BeTrue();  
                var currentToken = client.Auth.CurrentToken;
                currentToken.Token.Should().Be(_returnedDummyTokenDetails.Token);
                currentToken.ClientId.Should().Be(_returnedDummyTokenDetails.ClientId);
                currentToken.Expires.Should().BeCloseTo(_returnedDummyTokenDetails.Expires);
                raisedErrors.Should().BeEmpty("No errors should be raised!");
            }

            [Fact]
            [Trait("spec", "RTN14b")]
            public async Task WithTokenErrorAndNonRenewableToken_ShouldRaiseErrorAndTransitionToFailed()
            {
                var tokenDetails = new TokenDetails("id") { Expires = Now.AddHours(1) };
                bool renewTokenCalled = false;
                var client = GetClientWithFakeTransport(opts =>
                {
                    opts.Key = "";
                    opts.TokenDetails = tokenDetails;
                    opts.UseBinaryProtocol = false;
                }, request =>
                {
                    if (request.Url.Contains("/keys"))
                    {
                        renewTokenCalled = true;
                        return _returnedDummyTokenDetails.ToJson().ToAblyResponse();
                    }

                    return AblyResponse.EmptyResponse.ToTask();
                });

                FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { error = new ErrorInfo("Unauthorised", _tokenErrorCode, HttpStatusCode.Unauthorized) });

                renewTokenCalled.Should().BeFalse();
                client.Connection.State.Should().Be(ConnectionStateType.Failed);
                client.Connection.Reason.Should().NotBeNull();
                client.Connection.Reason.code.Should().Be(_tokenErrorCode);
            }

            [Fact]
            [Trait("spec", "RTN14b")]
            public async Task WithTokenErrorAndTokenRenewalFails_ShouldRaiseErrorAndTransitionToFailed()
            {
                var tokenDetails = new TokenDetails("id") { Expires = Now.AddHours(1) };
                var client = GetClientWithFakeTransport(opts =>
                {
                    opts.TokenDetails = tokenDetails;
                    opts.UseBinaryProtocol = false;
                }, request =>
                {
                    if (request.Url.Contains("/keys"))
                    {
                        throw new AblyException(new ErrorInfo() { code = 123});
                    }

                    return AblyResponse.EmptyResponse.ToTask();
                });

                FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { error = new ErrorInfo("Unauthorised", _tokenErrorCode, HttpStatusCode.Unauthorized) });

                client.Connection.State.Should().Be(ConnectionStateType.Failed);
                client.Connection.Reason.Should().NotBeNull();
                client.Connection.Reason.code.Should().Be(123);
            }

            [Fact]
            [Trait("spec", "RTN14b")]
            public async Task WithTokenErrorTwice_ShouldNotRenewAndRaiseErrorAndTransitionToFailed()
            {
                var tokenDetails = new TokenDetails("id") { Expires = Now.AddHours(1) };
                var renewCount = 0;
                var client = GetClientWithFakeTransport(opts =>
                {
                    opts.TokenDetails = tokenDetails;
                    opts.UseBinaryProtocol = false;
                }, request =>
                {
                    if (request.Url.Contains("/keys"))
                    {
                        renewCount++;
                        return _returnedDummyTokenDetails.ToJson().ToAblyResponse();
                    }

                    return AblyResponse.EmptyResponse.ToTask();
                });

                FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { error = new ErrorInfo("Unauthorised", _tokenErrorCode, HttpStatusCode.Unauthorized) });
                FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { error = new ErrorInfo("Unauthorised", _tokenErrorCode, HttpStatusCode.Unauthorized) });

                renewCount.Should().Be(1);
                client.Connection.State.Should().Be(ConnectionStateType.Failed);
                client.Connection.Reason.Should().NotBeNull();
            }

            public ConnectionFailureSpecs(ITestOutputHelper output) : base(output)
            {
            }
        }
    }
}
