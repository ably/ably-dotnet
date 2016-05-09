using States = IO.Ably.Transport.States.Connection;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Auth;
using IO.Ably.Realtime;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    //Temporarily made private to fix the Rest unit tests first
    class ConnectionManagerTests : AblySpecs
    {
        [Fact]
        public void When_Created_StateIsInitialized()
        {
            // Arrange
            IConnectionContext target = new ConnectionManager(null);

            // Assert
            Assert.Equal<ConnectionStateType>(ConnectionStateType.Initialized, target.State.State);
        }

        [Theory]
        [InlineData(ProtocolMessage.MessageAction.Heartbeat)]
        [InlineData(ProtocolMessage.MessageAction.Ack)]
        [InlineData(ProtocolMessage.MessageAction.Nack)]
        [InlineData(ProtocolMessage.MessageAction.Connect)]
        [InlineData(ProtocolMessage.MessageAction.Connected)]
        [InlineData(ProtocolMessage.MessageAction.Disconnect)]
        [InlineData(ProtocolMessage.MessageAction.Disconnected)]
        [InlineData(ProtocolMessage.MessageAction.Close)]
        [InlineData(ProtocolMessage.MessageAction.Closed)]
        [InlineData(ProtocolMessage.MessageAction.Error)]
        [InlineData(ProtocolMessage.MessageAction.Attach)]
        [InlineData(ProtocolMessage.MessageAction.Attached)]
        [InlineData(ProtocolMessage.MessageAction.Detach)]
        [InlineData(ProtocolMessage.MessageAction.Detached)]
        [InlineData(ProtocolMessage.MessageAction.Presence)]
        [InlineData(ProtocolMessage.MessageAction.Message)]
        [InlineData(ProtocolMessage.MessageAction.Sync)]
        public void ListensForMessages_CallMessageReceived(ProtocolMessage.MessageAction action)
        {
            // Arrange
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupProperty(c => c.Listener);
            ConnectionManager manager = new ConnectionManager(transport.Object, new AcknowledgementProcessor(), new ConnectionInitializedState(null), GetRestClient());
            List<ProtocolMessage> res = new List<ProtocolMessage>();
            manager.MessageReceived += (message) => res.Add(message);
            ProtocolMessage target = new ProtocolMessage(action);

            // Act
            transport.Object.Listener.OnTransportDataReceived(target);

            // Assert
            Assert.Single(res, target);
        }

        #region StateCommunication

        [Fact]
        public void WhenTransportConnected_StateCallbackCalled()
        {
            // Arrange
            Mock<Transport.States.Connection.ConnectionState> state = new Mock<Transport.States.Connection.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Closing);
            transport.SetupProperty(c => c.Listener);
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object, GetRestClient());

            // Act
            transport.Object.Listener.OnTransportConnected();

            // Assert
            state.Verify(c => c.OnTransportStateChanged(It.Is<Transport.States.Connection.ConnectionState.TransportStateInfo>(ss => ss.State == TransportState.Connected)), Times.Once());
        }

        [Fact]
        public void WhenTransportDisconnected_StateCallbackCalled()
        {
            // Arrange
            Mock<Transport.States.Connection.ConnectionState> state = new Mock<Transport.States.Connection.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Closing);
            transport.SetupProperty(c => c.Listener);
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object, GetRestClient());

            // Act
            transport.Object.Listener.OnTransportDisconnected();

            // Assert
            state.Verify(c => c.OnTransportStateChanged(It.Is<Transport.States.Connection.ConnectionState.TransportStateInfo>(ss =>
                ss.State == TransportState.Closed)), Times.Once());
        }

        [Fact]
        public void WhenTransportError_StateCallbackCalled()
        {
            // Arrange
            Mock<Transport.States.Connection.ConnectionState> state = new Mock<Transport.States.Connection.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Closed);
            transport.SetupProperty(c => c.Listener);
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object, GetRestClient());
            Exception targetError = new Exception("test");

            // Act
            transport.Object.Listener.OnTransportError(targetError);

            // Assert
            state.Verify(c => c.OnTransportStateChanged(It.Is<Transport.States.Connection.ConnectionState.TransportStateInfo>(ss =>
                ss.Error == targetError && ss.State == TransportState.Closed)), Times.Once());
        }

        [Fact]
        public void WhenTransportMessageReceived_StateCallbackCalled()
        {
            // Arrange
            Mock<Transport.States.Connection.ConnectionState> state = new Mock<Transport.States.Connection.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Closed);
            transport.SetupProperty(c => c.Listener);
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object, GetRestClient());
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message);

            // Act
            transport.Object.Listener.OnTransportDataReceived(targetMessage);

            // Assert
            state.Verify(c => c.OnMessageReceived(targetMessage), Times.Once());
        }

        [Fact]
        public void WhenConnect_StateCallbackCalled()
        {
            // Arrange
            Mock<Transport.States.Connection.ConnectionState> state = new Mock<Transport.States.Connection.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object, GetRestClient());

            // Act
            target.Connect();

            // Assert
            state.Verify(c => c.Connect(), Times.Once());
        }

        [Fact]
        public void WhenClose_StateCallbackCalled()
        {
            // Arrange
            Mock<Transport.States.Connection.ConnectionState> state = new Mock<Transport.States.Connection.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object, GetRestClient());

            // Act
            target.Close();

            // Assert
            state.Verify(c => c.Close(), Times.Once());
        }

        [Fact]
        public void WhenSend_StateCallbackCalled()
        {
            // Arrange
            Mock<Transport.States.Connection.ConnectionState> state = new Mock<Transport.States.Connection.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object, GetRestClient());
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message);

            // Act
            target.Send(targetMessage, null);

            // Assert
            state.Verify(c => c.SendMessage(targetMessage), Times.Once());
        }

        [Fact]
        public void WhenSetState_OnAttachedToContextCalled()
        {
            // Arrange
            Mock<Transport.States.Connection.ConnectionState> state = new Mock<Transport.States.Connection.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            IConnectionContext target = new ConnectionManager(transport.Object, ackProcessor.Object, new ConnectionInitializedState(null), GetRestClient());
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message);

            // Act
            target.SetState(state.Object);

            // Assert
            state.Verify(c => c.OnAttachedToContext(), Times.Once());
        }

        #endregion

        #region AckProcessorCommunication

        [Fact]
        public void WhenTransportMessageReceived_AckCallbackCalled()
        {
            // Arrange
            Mock<Transport.States.Connection.ConnectionState> state = new Mock<Transport.States.Connection.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Closed);
            transport.SetupProperty(c => c.Listener);
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object, GetRestClient());
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message);

            // Act
            transport.Object.Listener.OnTransportDataReceived(targetMessage);

            // Assert
            ackProcessor.Verify(c => c.OnMessageReceived(targetMessage), Times.Once());
        }

        [Fact]
        public void WhenSetState_AckCallbackCalled()
        {
            // Arrange
            Mock<Transport.States.Connection.ConnectionState> state = new Mock<Transport.States.Connection.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            IConnectionContext target = new ConnectionManager(transport.Object, ackProcessor.Object, new ConnectionInitializedState(null), GetRestClient());
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message);

            // Act
            target.SetState(state.Object);

            // Assert
            ackProcessor.Verify(c => c.OnStateChanged(state.Object), Times.Once());
        }

        [Fact]
        public void WhenSend_AckCallbackCalled()
        {
            // Arrange
            Mock<Transport.States.Connection.ConnectionState> state = new Mock<Transport.States.Connection.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object, GetRestClient());
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message);
            Action<bool, ErrorInfo> targetAction = (b, e) => { };

            // Act
            target.Send(targetMessage, targetAction);

            // Assert
            ackProcessor.Verify(c => c.SendMessage(targetMessage, targetAction), Times.Once());
        }

        [Fact]
        public void WhenSend_AckCalledBeforeState()
        {
            // Arrange
            Mock<Transport.States.Connection.ConnectionState> state = new Mock<Transport.States.Connection.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            string firstCall = null;
            ackProcessor.Setup(c => c.SendMessage(It.IsAny<ProtocolMessage>(), It.IsAny<Action<bool, ErrorInfo>>()))
                .Callback(() => { if (firstCall == null) firstCall = "ack"; });
            state.Setup(c => c.SendMessage(It.IsAny<ProtocolMessage>()))
                .Callback(() => { if (firstCall == null) firstCall = "state"; });
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object, GetRestClient());
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message);
            Action<bool, ErrorInfo> targetAction = (b, e) => { };

            // Act
            target.Send(targetMessage, targetAction);

            // Assert
            Assert.Equal<String>("ack", firstCall);
        }

        #endregion

        public class TransportParameterTests : AblySpecs
        {
            private FakeAuth _fakeAuth;
            private TransportParams _params;
            private Dictionary<string, string> _queryParams; 
            public TransportParameterTests(ITestOutputHelper output) : base(output)
            {
                _fakeAuth = new FakeAuth();
            }

            private async Task Create(ClientOptions options = null, string connectionKey = null, long? connectionSerial = null)
            {
                options = options ?? new ClientOptions();
                _params = await TransportParams.Create(_fakeAuth, options, connectionKey, connectionSerial);
                _queryParams = _params.GetParams();
            }

            [Fact]
            public async Task ShouldUseCorrectKey()
            {
                // Arrange
                await Create(connectionKey: "123");

                // Assert
                _params.ConnectionKey.Should().Be("123");
            }

            [Fact]
            public async Task ShouldUseCorrectSerial()
            {
                // Arrange
                await Create(connectionSerial: 123);

                // Assert
                _params.ConnectionSerial.Should().Be(123);
            }

            [Fact]
            public async Task ShouldSetDefaultHost()
            {
                // Arrange
                await Create();

                // Assert
                _params.Host.Should().Be(Defaults.RealtimeHost);
            }

            [Fact]
            public async Task When_HostSetInOptions_ShouldHaveCorrectHost()
            {
                // Arrange
                ClientOptions options = new ClientOptions();
                options.RealtimeHost = "test.test.com";
                await Create(options);

                _params.Host.Should().Be("test.test.com");
            }

            [Theory]
            [InlineData(AblyEnvironment.Sandbox)]
            public async Task When_EnvironmentSetInOptions_CreateCorrectTransportParameters(AblyEnvironment environment)
            {
                // Arrange
                ClientOptions options = new ClientOptions();
                options.Environment = environment;
                options.RealtimeHost = "test";
                await Create(options);

                // Assert
                Assert.Equal<string>(string.Format("{0}-{1}", environment, options.RealtimeHost).ToLower(), _params.Host);
            }

            
            [Fact]
            public async Task When_EnvironmentSetInOptions_Live_FallbackDoesNotModifyIt()
            {
                // Arrange
                ClientOptions options = new ClientOptions();
                options.Environment = AblyEnvironment.Live;
                options.RealtimeHost = "test";
                await Create(options);

                // Assert
                Assert.Equal<string>(options.RealtimeHost, _params.Host);
            }

            [Fact]
            public async Task WithBasicAuth_ShouldAddKeyToQuery()
            {
                _fakeAuth.AuthMethod = AuthMethod.Basic;
                await Create(new ClientOptions(ValidKey));

                _queryParams["key"].Should().Be(ValidKey);
            }

            [Fact]
            public async Task WithTokenAuth_ShouldAddAccessTokenToQuery()
            {
                // Arrange
                _fakeAuth.AuthMethod = AuthMethod.Token;
                _fakeAuth.CurrentToken = new TokenDetails("123");
                await Create();

                //Assert
                _queryParams["access_token"].Should().Be("123");
            }

            [Fact]
            public async Task WithUseBinaryProtocol_ShouldAddMsgPackFormatToQuery()
            {
                // Arrange
                await Create(new ClientOptions() { UseBinaryProtocol = true});

                _queryParams["format"].Should().Be("msgpack");
            }

            [Fact]
            public async Task WithUseBinaryProtocolFalse_ShouldNotAddFormatToQuery()
            {
                // Arrange
                await Create(new ClientOptions() {UseBinaryProtocol = false});
                // Act

                _queryParams.Should().NotContain("format", "");
            }

            [Fact]
            public async Task WithConnectionKey_ShouldSetResumeToQuery()
            {
                // Arrange
                string target = "123456789";
                await Create(null, target);

                _queryParams["resume"].Should().Be(target);
            }

            [Fact]
            public async Task WithConnectionKeyAndSerial_ShouldSetCorrectQueryValues()
            {
                // Arrange
                string target = "123456789";
                await Create(null, "123", 123456);

                // Assert
                _queryParams["connection_serial"].Should().Be("123456");
            }

            [Fact]
            public async Task WithRecoverOption_ShouldSetRecoverConnectionSerialToQuery()
            {
                // Arrange
                string target = "test-:123";
                await Create(new ClientOptions() {Recover = target});

                _queryParams["recover"].Should().Be("test-");
                _queryParams["connection_serial"].Should().Be("123");
            }

            [Fact]
            public async Task WithClientId_ShouldAddClientIdToQuery()
            {
                // Arrange
                string target = "test123";
                await Create(new ClientOptions() {ClientId = "tast123"});

                // Assert
                _queryParams["clientId"].Should().Be("test123");
            }
        }
        

        #region Connection

        [Fact]
        public async Task ConnectionPing_Calls_ConnectionManager_Ping()
        {
            // Arrange
            Mock<ITransport> mock = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackmock = new Mock<IAcknowledgementProcessor>();
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            state.Setup(c => c.State).Returns(ConnectionStateType.Connected);
            ConnectionManager target = new ConnectionManager(mock.Object, ackmock.Object, state.Object, GetRestClient());

            // Act
            await target.PingAsync();

            // Assert
            state.Verify(c => c.SendMessage(It.Is<ProtocolMessage>(m => m.action == ProtocolMessage.MessageAction.Heartbeat)), Times.Once());
        }

        [Fact]
        public void ConnectionConnect_Calls_ConnectionManager_Connect()
        {
            // Arrange
            Mock<IConnectionManager> mock = new Mock<IConnectionManager>();
            Connection target = new Connection(mock.Object);

            // Act
            target.Connect();

            // Assert
            mock.Verify(c => c.Connect(), Times.Once());
        }

        [Fact]
        public void ConnectionClose_Calls_ConnectionManager_Close()
        {
            // Arrange
            Mock<IConnectionManager> mock = new Mock<IConnectionManager>();
            Connection target = new Connection(mock.Object);

            // Act
            target.Close();

            // Assert
            mock.Verify(c => c.Close(), Times.Once());
        }

        #endregion

        #region ConnectionHeartbeatRequest tests

        [Theory]
        [InlineData(ConnectionStateType.Closed)]
        [InlineData(ConnectionStateType.Closing)]
        [InlineData(ConnectionStateType.Connecting)]
        [InlineData(ConnectionStateType.Disconnected)]
        [InlineData(ConnectionStateType.Failed)]
        [InlineData(ConnectionStateType.Initialized)]
        [InlineData(ConnectionStateType.Suspended)]
        public void ConnectionHeartbeatRequest_FailsWhenNotConnected(ConnectionStateType state)
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.Setup(c => c.ConnectionState).Returns(state);
            manager.Setup(c => c.Connection).Returns(new Connection(manager.Object));
            var res = new List<Tuple<TimeSpan?, ErrorInfo>>();
            Action<TimeSpan?, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, null, callback);

            // Assert
            Assert.Equal<int>(1, res.Count);
            Assert.Null(res[0].Item1);
            Assert.NotNull(res[0].Item2);
        }

        [Theory]
        [InlineData(ConnectionStateType.Closed)]
        [InlineData(ConnectionStateType.Closing)]
        [InlineData(ConnectionStateType.Connecting)]
        [InlineData(ConnectionStateType.Disconnected)]
        [InlineData(ConnectionStateType.Failed)]
        [InlineData(ConnectionStateType.Initialized)]
        [InlineData(ConnectionStateType.Suspended)]
        public void ConnectionHeartbeatRequest_FailsWhenNotConnected_WithNoCallback(ConnectionStateType state)
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.Setup(c => c.ConnectionState).Returns(state);
            manager.Setup(c => c.Connection).Returns(new Connection(manager.Object));

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, null, null);
        }

        [Fact]
        public void ConnectionHeartbeatRequest_WithNoCallback_SendsMessage()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.Setup(c => c.ConnectionState).Returns(ConnectionStateType.Connected);
            manager.Setup(c => c.Connection).Returns(new Connection(manager.Object));

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, null, null);

            // Assert
            manager.Verify(c => c.Send(It.Is<ProtocolMessage>(ss => ss.action == ProtocolMessage.MessageAction.Heartbeat), null), Times.Once());
        }

        [Fact]
        public void ConnectionHeartbeatRequest_WithNoCallback_DoesNotListenForMessages()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.Setup(c => c.ConnectionState).Returns(ConnectionStateType.Connected);
            manager.Setup(c => c.Connection).Returns(new Connection(manager.Object));

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, null, null);
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));
        }

        [Fact]
        public void ConnectionHeartbeatRequest_WithNoCallback_DoesNotListenForStateChanges()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<Connection> connection = new Mock<Connection>();
            connection.Setup(c => c.State).Returns(ConnectionStateType.Connected);
            manager.Setup(c => c.ConnectionState).Returns(ConnectionStateType.Connected);
            manager.Setup(c => c.Connection).Returns(connection.Object);

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, null, null);
            connection.Raise(c => c.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(ConnectionStateType.Closed, ConnectionStateType.Connected, null, null));
        }

        [Fact]
        public void ConnectionHeartbeatRequest_SendsMessage()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<ICountdownTimer> timer = new Mock<ICountdownTimer>();
            manager.Setup(c => c.ConnectionState).Returns(ConnectionStateType.Connected);
            manager.Setup(c => c.Connection).Returns(new Connection(manager.Object));

            var res = new List<Tuple<TimeSpan?, ErrorInfo>>();
            Action<TimeSpan?, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, timer.Object, callback);

            // Assert
            manager.Verify(c => c.Send(It.Is<ProtocolMessage>(ss => ss.action == ProtocolMessage.MessageAction.Heartbeat), null), Times.Once());
            Assert.Empty(res);
        }

        [Fact]
        public void ConnectionHeartbeatRequest_StartsTimer()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<ICountdownTimer> timer = new Mock<ICountdownTimer>();
            manager.Setup(c => c.ConnectionState).Returns(ConnectionStateType.Connected);
            manager.Setup(c => c.Connection).Returns(new Connection(manager.Object));
            List<Tuple<TimeSpan?, ErrorInfo>> res = new List<Tuple<TimeSpan?, ErrorInfo>>();
            Action<TimeSpan?, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, timer.Object, callback);

            // Assert
            timer.Verify(c => c.Start(It.IsAny<TimeSpan>(), It.IsAny<Action>(), false), Times.Once());
            Assert.Empty(res);
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
        [InlineData(ProtocolMessage.MessageAction.Message)]
        [InlineData(ProtocolMessage.MessageAction.Nack)]
        [InlineData(ProtocolMessage.MessageAction.Presence)]
        [InlineData(ProtocolMessage.MessageAction.Sync)]
        public void ConnectionHeartbeatRequest_ListensForMessages_DoesNotCallback(ProtocolMessage.MessageAction action)
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<ICountdownTimer> timer = new Mock<ICountdownTimer>();
            manager.Setup(c => c.ConnectionState).Returns(ConnectionStateType.Connected);
            manager.Setup(c => c.Connection).Returns(new Connection(manager.Object));
            List<Tuple<TimeSpan?, ErrorInfo>> res = new List<Tuple<TimeSpan?, ErrorInfo>>();
            Action<TimeSpan?, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, timer.Object, callback);
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(action));

            // Assert
            Assert.Empty(res);
        }

        [Fact]
        public void ConnectionHeartbeatRequest_ListensForMessages_Heartbeat()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<ICountdownTimer> timer = new Mock<ICountdownTimer>();
            manager.Setup(c => c.ConnectionState).Returns(ConnectionStateType.Connected);
            manager.Setup(c => c.Connection).Returns(new Connection(manager.Object));
            List<Tuple<TimeSpan?, ErrorInfo>> res = new List<Tuple<TimeSpan?, ErrorInfo>>();
            Action<TimeSpan?, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, timer.Object, callback);
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));

            // Assert
            Assert.Equal<int>(1, res.Count);
            Assert.Null(res[0].Item1);
            Assert.Null(res[0].Item2);
        }

        [Fact]
        public void ConnectionHeartbeatRequest_ListensForMessages_Heartbeat_StopsTimer()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<ICountdownTimer> timer = new Mock<ICountdownTimer>();
            manager.Setup(c => c.ConnectionState).Returns(ConnectionStateType.Connected);
            manager.Setup(c => c.Connection).Returns(new Connection(manager.Object));
            List<Tuple<TimeSpan?, ErrorInfo>> res = new List<Tuple<TimeSpan?, ErrorInfo>>();
            Action<TimeSpan?, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, timer.Object, callback);
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));

            // Assert
            timer.Verify(c => c.Abort(), Times.Once());
        }

        [Fact]
        public void ConnectionHeartbeatRequest_ListensForMessages_Heartbeat_Unsubscribes()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<ICountdownTimer> timer = new Mock<ICountdownTimer>();
            Mock<Connection> connection = new Mock<Connection>();
            connection.Setup(c => c.State).Returns(ConnectionStateType.Connected);
            manager.Setup(c => c.ConnectionState).Returns(ConnectionStateType.Connected);
            manager.Setup(c => c.Connection).Returns(connection.Object);
            var res = new List<Tuple<TimeSpan?, ErrorInfo>>();
            Action<TimeSpan?, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, timer.Object, callback);
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));
            connection.Raise(c => c.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(ConnectionStateType.Closing, ConnectionStateType.Closed));
            connection.Raise(c => c.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(ConnectionStateType.Closing, ConnectionStateType.Closed));
            connection.Raise(c => c.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(ConnectionStateType.Closing, ConnectionStateType.Closed));
            connection.Raise(c => c.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(ConnectionStateType.Closing, ConnectionStateType.Closed));

            // Assert
            Assert.Equal<int>(1, res.Count);
        }

        [Theory]
        [InlineData(ConnectionStateType.Closed)]
        [InlineData(ConnectionStateType.Closing)]
        [InlineData(ConnectionStateType.Connecting)]
        [InlineData(ConnectionStateType.Disconnected)]
        [InlineData(ConnectionStateType.Failed)]
        [InlineData(ConnectionStateType.Initialized)]
        [InlineData(ConnectionStateType.Suspended)]
        public void ConnectionHeartbeatRequest_ListensForStateChanges(ConnectionStateType state)
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<ICountdownTimer> timer = new Mock<ICountdownTimer>();
            Mock<Connection> connection = new Mock<Connection>();
            connection.Setup(c => c.State).Returns(ConnectionStateType.Connected);
            manager.Setup(c => c.ConnectionState).Returns(ConnectionStateType.Connected);
            manager.Setup(c => c.Connection).Returns(connection.Object);
            var res = new List<Tuple<TimeSpan?, ErrorInfo>>();
            Action<TimeSpan?, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, timer.Object, callback);
            connection.Raise(c => c.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(state, state));

            // Assert
            Assert.Equal<int>(1, res.Count);
            Assert.Null(res[0].Item1);
            Assert.NotNull(res[0].Item2);
        }

        [Theory]
        [InlineData(ConnectionStateType.Closed)]
        [InlineData(ConnectionStateType.Closing)]
        [InlineData(ConnectionStateType.Connecting)]
        [InlineData(ConnectionStateType.Disconnected)]
        [InlineData(ConnectionStateType.Failed)]
        [InlineData(ConnectionStateType.Initialized)]
        [InlineData(ConnectionStateType.Suspended)]
        public void ConnectionHeartbeatRequest_ListensForStateChanges_StopsTimer(ConnectionStateType state)
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<ICountdownTimer> timer = new Mock<ICountdownTimer>();
            Mock<Connection> connection = new Mock<Connection>();
            connection.SetupGet(c => c.State).Returns(ConnectionStateType.Connected);
            manager.Setup(c => c.ConnectionState).Returns(ConnectionStateType.Connected);
            manager.Setup(c => c.Connection).Returns(connection.Object);
            Action<TimeSpan?, ErrorInfo> callback = (e, err) => { };

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, timer.Object, callback);
            connection.Raise(c => c.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(state, state));

            // Assert
            timer.Verify(c => c.Abort(), Times.Once());
        }

        [Theory]
        [InlineData(ConnectionStateType.Closed)]
        [InlineData(ConnectionStateType.Closing)]
        [InlineData(ConnectionStateType.Connecting)]
        [InlineData(ConnectionStateType.Disconnected)]
        [InlineData(ConnectionStateType.Failed)]
        [InlineData(ConnectionStateType.Initialized)]
        [InlineData(ConnectionStateType.Suspended)]
        public void ConnectionHeartbeatRequest_ListensForStateChanges_Unsubscribes(ConnectionStateType state)
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<ICountdownTimer> timer = new Mock<ICountdownTimer>();
            Mock<Connection> connection = new Mock<Connection>();
            connection.Setup(c => c.State).Returns(ConnectionStateType.Connected);
            manager.Setup(c => c.ConnectionState).Returns(ConnectionStateType.Connected);
            manager.Setup(c => c.Connection).Returns(connection.Object);
            var res = new List<Tuple<TimeSpan?, ErrorInfo>>();
            Action<TimeSpan?, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, timer.Object, callback);
            connection.Raise(c => c.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(state, state));
            connection.Raise(c => c.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(state, state));
            connection.Raise(c => c.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(state, state));
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));

            // Assert
            Assert.Equal<int>(1, res.Count);
        }

        [Fact]
        public void ConnectionHeartbeatRequest_TimesOut()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<ICountdownTimer> timer = new Mock<ICountdownTimer>();
            Mock<Connection> connection = new Mock<Connection>();
            connection.Setup(c => c.State).Returns(ConnectionStateType.Connected);
            manager.Setup(c => c.ConnectionState).Returns(ConnectionStateType.Connected);
            manager.Setup(c => c.Connection).Returns(connection.Object);
            var res = new List<Tuple<TimeSpan?, ErrorInfo>>();
            Action<TimeSpan?, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));
            timer.Setup(c => c.Start(It.IsAny<TimeSpan>(), It.IsAny<Action>(), false)).Callback<int, Action>((c, e) => e());

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, timer.Object, callback);

            // Assert
            Assert.Equal<int>(1, res.Count);
            Assert.Null(res[0].Item1);
            Assert.NotNull(res[0].Item2);
        }

        [Fact]
        public void ConnectionHeartbeatRequest_TimeOut_Unsubscribe()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<ICountdownTimer> timer = new Mock<ICountdownTimer>();
            Mock<Connection> connection = new Mock<Connection>();
            connection.Setup(c => c.State).Returns(ConnectionStateType.Connected);
            manager.Setup(c => c.ConnectionState).Returns(ConnectionStateType.Connected);
            manager.Setup(c => c.Connection).Returns(connection.Object);
            var res = new List<Tuple<TimeSpan?, ErrorInfo>>();
            Action<TimeSpan?, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));
            timer.Setup(c => c.Start(It.IsAny<TimeSpan>(), It.IsAny<Action>(), false)).Callback<int, Action>((c, e) => e());

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, timer.Object, callback);
            connection.Raise(c => c.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(ConnectionStateType.Closed, ConnectionStateType.Closed));
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));

            // Assert
            Assert.Equal<int>(1, res.Count);
            Assert.Null(res[0].Item1);
            Assert.NotNull(res[0].Item2);
        }

        [Fact]
        public void ConnectionHeartbeatRequest_MultipleRequests()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<ICountdownTimer> timer = new Mock<ICountdownTimer>();
            manager.Setup(c => c.ConnectionState).Returns(ConnectionStateType.Connected);
            manager.Setup(c => c.Connection).Returns(new Connection(manager.Object));
            List<Tuple<TimeSpan?, ErrorInfo>> res = new List<Tuple<TimeSpan?, ErrorInfo>>();
            Action<TimeSpan?, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));
            const int count = 10;

            // Act
            for (int i = 0; i < count; i++)
            {
                ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, timer.Object, callback);
            }
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));

            // Assert
            Assert.Equal<int>(count, res.Count);
            foreach (var item in res)
            {
                Assert.NotNull(item.Item1);
                Assert.Null(item.Item2);
            }
        }

        #endregion

        

        private AblyRest GetRestClient()
        {
            return new AblyRest(ValidKey);
        }

        public ConnectionManagerTests(ITestOutputHelper output) : base(output)
        {
        }
    }
}
