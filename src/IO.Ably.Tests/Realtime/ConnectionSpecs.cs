using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
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

        protected AblyRealtime GetClientWithFakeTransport(Action<ClientOptions> optionsAction = null)
        {
            var options = new ClientOptions(ValidKey) { TransportFactory = _fakeTransportFactory };
            optionsAction?.Invoke(options);
            var client = GetRealtimeClient(options);
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
                var clientId = "123";
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
    }
}
