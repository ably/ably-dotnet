using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Types;
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
            [Trait("sandboxTest", "needed")]
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
            [Trait("sandboxTest", "needed")]
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
            [Trait("sandboxTest", "needed")]
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

                await LastCreatedTransport.Listener.OnTransportMessageReceived(
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
            [Theory]
            [InlineData(ProtocolMessage.MessageAction.Message)]
            [InlineData(ProtocolMessage.MessageAction.Presence)]
            public void WhenSendingMessage_IncrementsMsgSerial(ProtocolMessage.MessageAction messageAction)
            {
                // Arrange
                AcknowledgementProcessor target = new AcknowledgementProcessor();
                ProtocolMessage targetMessage1 = new ProtocolMessage(messageAction, "Test");
                ProtocolMessage targetMessage2 = new ProtocolMessage(messageAction, "Test");
                ProtocolMessage targetMessage3 = new ProtocolMessage(messageAction, "Test");

                // Act
                target.SendMessage(targetMessage1, null);
                target.SendMessage(targetMessage2, null);
                target.SendMessage(targetMessage3, null);

                // Assert
                Assert.Equal(0, targetMessage1.msgSerial);
                Assert.Equal(1, targetMessage2.msgSerial);
                Assert.Equal(2, targetMessage3.msgSerial);
            }

            [Theory]
            [InlineData(ProtocolMessage.MessageAction.Ack)]
            [InlineData(ProtocolMessage.MessageAction.Attach)]
            [InlineData(ProtocolMessage.MessageAction.Attached)]
            [InlineData(ProtocolMessage.MessageAction.Close)]
            [InlineData(ProtocolMessage.MessageAction.Closed)]
            [InlineData(ProtocolMessage.MessageAction.Connect)]
            [InlineData(ProtocolMessage.MessageAction.Connected)]
            [InlineData(ProtocolMessage.MessageAction.Detach)]
            [InlineData(ProtocolMessage.MessageAction.Detached)]
            [InlineData(ProtocolMessage.MessageAction.Disconnect)]
            [InlineData(ProtocolMessage.MessageAction.Disconnected)]
            [InlineData(ProtocolMessage.MessageAction.Error)]
            [InlineData(ProtocolMessage.MessageAction.Heartbeat)]
            [InlineData(ProtocolMessage.MessageAction.Nack)]
            [InlineData(ProtocolMessage.MessageAction.Sync)]
            public void WhenSendingMessage_MsgSerialNotIncremented(ProtocolMessage.MessageAction messageAction)
            {
                // Arrange
                AcknowledgementProcessor target = new AcknowledgementProcessor();
                ProtocolMessage targetMessage1 = new ProtocolMessage(messageAction, "Test");
                ProtocolMessage targetMessage2 = new ProtocolMessage(messageAction, "Test");
                ProtocolMessage targetMessage3 = new ProtocolMessage(messageAction, "Test");

                // Act
                target.SendMessage(targetMessage1, null);
                target.SendMessage(targetMessage2, null);
                target.SendMessage(targetMessage3, null);

                // Assert
                Assert.Equal(0, targetMessage1.msgSerial);
                Assert.Equal(0, targetMessage2.msgSerial);
                Assert.Equal(0, targetMessage3.msgSerial);
            }

            [Theory]
            [InlineData(ProtocolMessage.MessageAction.Ack)]
            [InlineData(ProtocolMessage.MessageAction.Nack)]
            public void WhenReceiveMessage_HandleAction(ProtocolMessage.MessageAction action)
            {
                // Arrange
                AcknowledgementProcessor target = new AcknowledgementProcessor();

                // Act
                bool result = target.OnMessageReceived(new ProtocolMessage(action));

                // Assert
                Assert.True(result);
            }

            [Theory]
            [InlineData(ProtocolMessage.MessageAction.Attach)]
            [InlineData(ProtocolMessage.MessageAction.Attached)]
            [InlineData(ProtocolMessage.MessageAction.Close)]
            [InlineData(ProtocolMessage.MessageAction.Closed)]
            [InlineData(ProtocolMessage.MessageAction.Connect)]
            [InlineData(ProtocolMessage.MessageAction.Connected)]
            [InlineData(ProtocolMessage.MessageAction.Detach)]
            [InlineData(ProtocolMessage.MessageAction.Detached)]
            [InlineData(ProtocolMessage.MessageAction.Disconnect)]
            [InlineData(ProtocolMessage.MessageAction.Disconnected)]
            [InlineData(ProtocolMessage.MessageAction.Error)]
            [InlineData(ProtocolMessage.MessageAction.Heartbeat)]
            [InlineData(ProtocolMessage.MessageAction.Message)]
            [InlineData(ProtocolMessage.MessageAction.Presence)]
            [InlineData(ProtocolMessage.MessageAction.Sync)]
            public void WhenReceiveMessage_DoesNotHandleAction(ProtocolMessage.MessageAction action)
            {
                // Arrange
                AcknowledgementProcessor target = new AcknowledgementProcessor();

                // Act
                bool result = target.OnMessageReceived(new ProtocolMessage(action));

                // Assert
                Assert.False(result);
            }

            [Fact]
            public void WhenSendingMessage_AckCallbackCalled()
            {
                // Arrange
                AcknowledgementProcessor target = new AcknowledgementProcessor();
                List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();
                long msgSerial = 0;

                // Act
                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
                target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Ack) { msgSerial = msgSerial++, count = 1 });

                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
                target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Ack) { msgSerial = msgSerial++, count = 1 });

                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });
                target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Ack) { msgSerial = msgSerial++, count = 1 });

                // Assert
                Assert.Equal(3, callbacks.Count);
                Assert.True(callbacks.TrueForAll(c => c.Item1)); // Ack
                Assert.True(callbacks.TrueForAll(c => c.Item2 == null)); // No error
            }

            [Fact]
            public void WhenSendingMessage_AckCallbackCalled_ForMultipleMessages()
            {
                // Arrange
                AcknowledgementProcessor target = new AcknowledgementProcessor();
                List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();

                // Act
                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });
                target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Ack) { msgSerial = 0, count = 3 });

                // Assert
                Assert.Equal(3, callbacks.Count);
                Assert.True(callbacks.TrueForAll(c => c.Item1)); // Ack
                Assert.True(callbacks.TrueForAll(c => c.Item2 == null)); // No error
            }

            [Fact]
            public void WhenSendingMessage_NackCallbackCalled()
            {
                // Arrange
                AcknowledgementProcessor target = new AcknowledgementProcessor();
                List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();
                long msgSerial = 0;

                // Act
                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
                target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { msgSerial = msgSerial++, count = 1 });

                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
                target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { msgSerial = msgSerial++, count = 1 });

                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });
                target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { msgSerial = msgSerial++, count = 1 });

                // Assert
                Assert.Equal(3, callbacks.Count);
                Assert.True(callbacks.TrueForAll(c => !c.Item1)); // Nack
                Assert.True(callbacks.TrueForAll(c => c.Item2 != null)); // Error
            }

            [Fact]
            public void WhenSendingMessage_NackCallbackCalled_ForMultipleMessages()
            {
                // Arrange
                AcknowledgementProcessor target = new AcknowledgementProcessor();
                List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();

                // Act
                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });
                target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { msgSerial = 0, count = 3 });

                // Assert
                Assert.Equal(3, callbacks.Count);
                Assert.True(callbacks.TrueForAll(c => !c.Item1)); // Nack
                Assert.True(callbacks.TrueForAll(c => c.Item2 != null)); // Error
            }

            [Fact]
            public void WhenSendingMessage_NackCallbackCalled_WithError()
            {
                // Arrange
                AcknowledgementProcessor target = new AcknowledgementProcessor();
                List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();
                ErrorInfo error = new ErrorInfo("reason", 123);
                long msgSerial = 0;

                // Act
                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
                target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { msgSerial = msgSerial++, count = 1, error = error });

                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
                target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { msgSerial = msgSerial++, count = 1, error = error });

                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });
                target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { msgSerial = msgSerial++, count = 1, error = error });

                // Assert
                Assert.Equal(3, callbacks.Count);
                Assert.True(callbacks.TrueForAll(c => !c.Item1)); // Nack
                Assert.True(callbacks.TrueForAll(c => object.ReferenceEquals(c.Item2, error))); // Error
            }

            [Fact]
            public void WhenSendingMessage_NackCallbackCalled_ForMultipleMessages_WithError()
            {
                // Arrange
                AcknowledgementProcessor target = new AcknowledgementProcessor();
                List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();
                ErrorInfo error = new ErrorInfo("reason", 123);

                // Act
                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });
                target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { msgSerial = 0, count = 3, error = error });

                // Assert
                Assert.Equal(3, callbacks.Count);
                Assert.True(callbacks.TrueForAll(c => !c.Item1)); // Nack
                Assert.True(callbacks.TrueForAll(c => object.ReferenceEquals(c.Item2, error))); // Error
            }

            [Fact]
            public void OnState_Connected_MsgSerialReset()
            {
                // Arrange
                Mock<IConnectionContext> context = new Mock<IConnectionContext>();
                context.SetupGet(c => c.Connection).Returns(new Connection(new Mock<IConnectionManager>().Object));

                AcknowledgementProcessor target = new AcknowledgementProcessor();
                ProtocolMessage targetMessage1 = new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test");
                ProtocolMessage targetMessage2 = new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test");
                ProtocolMessage targetMessage3 = new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test");
                ProtocolMessage targetMessage4 = new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test");

                // Act
                target.SendMessage(targetMessage1, null);
                target.SendMessage(targetMessage2, null);
                target.OnStateChanged(new ConnectionConnectedState(context.Object, new ConnectionInfo("", 0, "", "")));
                target.SendMessage(targetMessage3, null);
                target.SendMessage(targetMessage4, null);

                // Assert
                Assert.Equal(0, targetMessage1.msgSerial);
                Assert.Equal(1, targetMessage2.msgSerial);
                Assert.Equal(0, targetMessage3.msgSerial);
                Assert.Equal(1, targetMessage4.msgSerial);
            }

            [Fact]
            public void OnState_Closed_FailCallbackCalled()
            {
                // Arrange
                AcknowledgementProcessor target = new AcknowledgementProcessor();
                List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();
                ErrorInfo error = new ErrorInfo("reason", 123);

                // Act
                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });
                target.OnStateChanged(new ConnectionClosedState(null, error));

                // Assert
                Assert.Equal(3, callbacks.Count);
                Assert.True(callbacks.TrueForAll(c => !c.Item1)); // Nack
                Assert.True(callbacks.TrueForAll(c => object.ReferenceEquals(c.Item2, error))); // Error
            }

            [Fact]
            public void OnState_Failed_FailCallbackCalled()
            {
                // Arrange
                AcknowledgementProcessor target = new AcknowledgementProcessor();
                List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();
                ErrorInfo error = new ErrorInfo("reason", 123);

                // Act
                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
                target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });
                target.OnStateChanged(new ConnectionFailedState(null, error));

                // Assert
                Assert.Equal(3, callbacks.Count);
                Assert.True(callbacks.TrueForAll(c => !c.Item1)); // Nack
                Assert.True(callbacks.TrueForAll(c => object.ReferenceEquals(c.Item2, error))); // Error
            }

            public AckNackSpecs(ITestOutputHelper output) : base(output)
            {
            }
        }
    }
}
