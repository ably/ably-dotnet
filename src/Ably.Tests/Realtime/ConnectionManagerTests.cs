using Ably.Realtime;
using Ably.Transport;
using States = Ably.Transport.States.Connection;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Extensions;
using Ably.Types;
using System.Net;

namespace Ably.Tests
{
    public class ConnectionManagerTests
    {
        [Fact]
        public void When_Created_StateIsInitialized()
        {
            // Arrange
            IConnectionContext target = new ConnectionManager(new AblyRealtimeOptions());

            // Assert
            Assert.Equal<ConnectionState>(ConnectionState.Initialized, target.State.State);
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
            ConnectionManager manager = new ConnectionManager(transport.Object, new AcknowledgementProcessor(), new States.ConnectionInitializedState(null));
            List<ProtocolMessage> res = new List<ProtocolMessage>();
            manager.MessageReceived += (message) => res.Add(message);
            ProtocolMessage target = new ProtocolMessage(action);

            // Act
            transport.Object.Listener.OnTransportMessageReceived(target);

            // Assert
            Assert.Single(res, target);
        }

        #region StateCommunication

        [Fact]
        public void WhenTransportConnected_StateCallbackCalled()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Closing);
            transport.SetupProperty(c => c.Listener);
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);

            // Act
            transport.Object.Listener.OnTransportConnected();

            // Assert
            state.Verify(c => c.OnTransportStateChanged(It.Is<States.ConnectionState.TransportStateInfo>(ss => ss.State == TransportState.Connected)), Times.Once());
        }

        [Fact]
        public void WhenTransportDisconnected_StateCallbackCalled()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Closing);
            transport.SetupProperty(c => c.Listener);
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);

            // Act
            transport.Object.Listener.OnTransportDisconnected();

            // Assert
            state.Verify(c => c.OnTransportStateChanged(It.Is<States.ConnectionState.TransportStateInfo>(ss =>
                ss.State == TransportState.Closed)), Times.Once());
        }

        [Fact]
        public void WhenTransportError_StateCallbackCalled()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Closed);
            transport.SetupProperty(c => c.Listener);
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);
            Exception targetError = new Exception("test");

            // Act
            transport.Object.Listener.OnTransportError(targetError);

            // Assert
            state.Verify(c => c.OnTransportStateChanged(It.Is<States.ConnectionState.TransportStateInfo>(ss =>
                ss.Error == targetError && ss.State == TransportState.Closed)), Times.Once());
        }

        [Fact]
        public void WhenTransportMessageReceived_StateCallbackCalled()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Closed);
            transport.SetupProperty(c => c.Listener);
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message);

            // Act
            transport.Object.Listener.OnTransportMessageReceived(targetMessage);

            // Assert
            state.Verify(c => c.OnMessageReceived(targetMessage), Times.Once());
        }

        [Fact]
        public void WhenConnect_StateCallbackCalled()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);

            // Act
            target.Connect();

            // Assert
            state.Verify(c => c.Connect(), Times.Once());
        }

        [Fact]
        public void WhenClose_StateCallbackCalled()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);

            // Act
            target.Close();

            // Assert
            state.Verify(c => c.Close(), Times.Once());
        }

        [Fact]
        public void WhenSend_StateCallbackCalled()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);
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
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            IConnectionContext target = new ConnectionManager(transport.Object, ackProcessor.Object, new States.ConnectionInitializedState(null));
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
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Closed);
            transport.SetupProperty(c => c.Listener);
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message);

            // Act
            transport.Object.Listener.OnTransportMessageReceived(targetMessage);

            // Assert
            ackProcessor.Verify(c => c.OnMessageReceived(targetMessage), Times.Once());
        }

        [Fact]
        public void WhenSetState_AckCallbackCalled()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            IConnectionContext target = new ConnectionManager(transport.Object, ackProcessor.Object, new States.ConnectionInitializedState(null));
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
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);
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
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            string firstCall = null;
            ackProcessor.Setup(c => c.SendMessage(It.IsAny<ProtocolMessage>(), It.IsAny<Action<bool, ErrorInfo>>()))
                .Callback(() => { if (firstCall == null) firstCall = "ack"; });
            state.Setup(c => c.SendMessage(It.IsAny<ProtocolMessage>()))
                .Callback(() => { if (firstCall == null) firstCall = "state"; });
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message);
            Action<bool, ErrorInfo> targetAction = (b, e) => { };

            // Act
            target.Send(targetMessage, targetAction);

            // Assert
            Assert.Equal<String>("ack", firstCall);
        }

        #endregion

        [Fact]
        public void CreateCorrectTransportParameters_UsesConnectionKey()
        {
            // Arrange
            AblyRealtimeOptions options = new AblyRealtimeOptions();
            Mock<Connection> connection = new Mock<Connection>();
            connection.SetupProperty(c => c.Key, "123");

            // Act
            TransportParams target = ConnectionManager.CreateTransportParameters(options, connection.Object, false);

            // Assert
            Assert.Equal<string>("123", target.ConnectionKey);
        }

        [Fact]
        public void CreateCorrectTransportParameters_UsesConnectionSerial()
        {
            // Arrange
            AblyRealtimeOptions options = new AblyRealtimeOptions();
            Mock<Connection> connection = new Mock<Connection>();
            connection.SetupProperty(c => c.Serial, 123);

            // Act
            TransportParams target = ConnectionManager.CreateTransportParameters(options, connection.Object, false);

            // Assert
            Assert.Equal<string>("123", target.ConnectionSerial);
        }

        [Fact]
        public void CreateCorrectTransportParameters_UsesDefaultHost()
        {
            // Arrange
            AblyRealtimeOptions options = new AblyRealtimeOptions();

            // Act
            TransportParams target = ConnectionManager.CreateTransportParameters(options, null, false);

            // Assert
            Assert.Equal<string>(Defaults.RealtimeHost, target.Host);
        }

        [Fact]
        public void CreateCorrectTransportParameters_Fallback_UsesFallbacktHost()
        {
            // Arrange
            AblyRealtimeOptions options = new AblyRealtimeOptions();

            // Act
            TransportParams target = ConnectionManager.CreateTransportParameters(options, null, true);

            // Assert
            Assert.True(Defaults.FallbackHosts.Contains(target.Host));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void When_HostSetInOptions_CreateTransportParameters_DoesNotModifyIt(bool fallback)
        {
            // Arrange
            AblyRealtimeOptions options = new AblyRealtimeOptions();
            options.Host = "http://test";

            // Act
            TransportParams target = ConnectionManager.CreateTransportParameters(options, null, fallback);

            // Assert
            Assert.Equal<string>(options.Host, target.Host);
        }

        [Theory]
        [InlineData(AblyEnvironment.Sandbox)]
        [InlineData(AblyEnvironment.Uat)]
        public void When_EnvironmentSetInOptions_CreateCorrectTransportParameters(AblyEnvironment environment)
        {
            // Arrange
            AblyRealtimeOptions options = new AblyRealtimeOptions();
            options.Environment = environment;
            options.Host = "test";

            // Act
            TransportParams target = ConnectionManager.CreateTransportParameters(options, null, false);

            // Assert
            Assert.Equal<string>(string.Format("{0}-{1}", environment, options.Host).ToLower(), target.Host);
        }

        [Theory]
        [InlineData(AblyEnvironment.Sandbox)]
        [InlineData(AblyEnvironment.Uat)]
        public void When_EnvironmentSetInOptions_CreateCorrectTransportParameters_Fallback(AblyEnvironment environment)
        {
            // Arrange
            AblyRealtimeOptions options = new AblyRealtimeOptions();
            options.Environment = environment;
            options.Host = "test";

            // Act
            TransportParams target = ConnectionManager.CreateTransportParameters(options, null, true);

            // Assert
            Assert.Equal<string>(string.Format("{0}-{1}", environment, options.Host).ToLower(), target.Host);
        }

        [Fact]
        public void When_EnvironmentSetInOptions_Live_CreateCorrectTransportParameters()
        {
            // Arrange
            AblyRealtimeOptions options = new AblyRealtimeOptions();
            options.Environment = AblyEnvironment.Live;
            options.Host = "test";

            // Act
            TransportParams target = ConnectionManager.CreateTransportParameters(options, null, false);

            // Assert
            Assert.Equal<string>(options.Host, target.Host);
        }

        [Fact]
        public void When_EnvironmentSetInOptions_Live_FallbackDoesNotModifyIt()
        {
            // Arrange
            AblyRealtimeOptions options = new AblyRealtimeOptions();
            options.Environment = AblyEnvironment.Live;
            options.Host = "test";

            // Act
            TransportParams target = ConnectionManager.CreateTransportParameters(options, null, true);

            // Assert
            Assert.Equal<string>(options.Host, target.Host);
        }

        [Fact]
        public void StoreTransportParams_Key()
        {
            string target = "123.456:789";
            TransportParams parameters = new TransportParams( new AblyRealtimeOptions( target ) );
            var table = new System.Net.WebHeaderCollection();

            parameters.StoreParams( table );

            Assert.Equal<string>( target, WebUtility.UrlDecode( table[ "key" ] ) );
        }

        [Fact]
        public void StoreTransportParams_Token()
        {
            // Arrange
            string target = "afafmasfasmsafnqwqff";
            TransportParams parameters = new TransportParams(new AblyRealtimeOptions() { Token = target });
            var table = new System.Net.WebHeaderCollection();

            // Act
            parameters.StoreParams(table);

            // Assert
            Assert.Equal<string>(target, table["access_token"]);
        }

        [Fact]
        public void StoreTransportParams_Format_MsgPack()
        {
            // Arrange
            TransportParams parameters = new TransportParams(new AblyRealtimeOptions() { UseBinaryProtocol = true });
            var table = new System.Net.WebHeaderCollection();

            // Act
            parameters.StoreParams(table);

            // Assert
            Assert.Equal<string>("msgpack", table["format"]);
        }

        [Fact]
        public void StoreTransportParams_Format_Text()
        {
            // Arrange
            TransportParams parameters = new TransportParams(new AblyRealtimeOptions() { UseBinaryProtocol = false });
            var table = new System.Net.WebHeaderCollection();
            // Act
            parameters.StoreParams(table);

            // Assert
            Assert.Null(table["format"]);
        }

        [Fact]
        public void StoreTransportParams_Resume()
        {
            // Arrange
            string target = "123456789";
            TransportParams parameters = new TransportParams(new AblyRealtimeOptions()) { ConnectionKey = target };
            var table = new System.Net.WebHeaderCollection();

            // Act
            parameters.StoreParams(table);

            // Assert
            Assert.Equal<Mode>(Mode.Resume, parameters.Mode);
        }

        [Fact]
        public void StoreTransportParams_Resume_Key()
        {
            // Arrange
            string target = "123456789";
            TransportParams parameters = new TransportParams(new AblyRealtimeOptions()) { ConnectionKey = target };
            var table = new System.Net.WebHeaderCollection();

            // Act
            parameters.StoreParams(table);

            // Assert
            Assert.Equal<string>(target, table["resume"]);
        }

        [Fact]
        public void StoreTransportParams_Resume_Serial()
        {
            // Arrange
            string target = "123456789";
            TransportParams parameters = new TransportParams(new AblyRealtimeOptions()) { ConnectionKey = "123", ConnectionSerial = target };
            var table = new System.Net.WebHeaderCollection();

            // Act
            parameters.StoreParams(table);

            // Assert
            Assert.Equal<string>(target, table["connection_serial"]);
        }

        [Fact]
        public void StoreTransportParams_Recover()
        {
            // Arrange
            string target = "test-:123";
            TransportParams parameters = new TransportParams(new AblyRealtimeOptions() { Recover = target });
            var table = new System.Net.WebHeaderCollection();

            // Act
            parameters.StoreParams(table);

            // Assert
            Assert.Equal<Mode>(Mode.Recover, parameters.Mode);
        }

        [Fact]
        public void StoreTransportParams_Recover_Key()
        {
            // Arrange
            string target = "test-:123";
            TransportParams parameters = new TransportParams(new AblyRealtimeOptions() { Recover = target });
            var table = new System.Net.WebHeaderCollection();

            // Act
            parameters.StoreParams(table);

            // Assert
            Assert.Equal<string>("test-", table["recover"]);
        }

        [Fact]
        public void StoreTransportParams_Recover_Serial()
        {
            // Arrange
            string target = "test-:123";
            TransportParams parameters = new TransportParams(new AblyRealtimeOptions() { Recover = target });
            var table = new System.Net.WebHeaderCollection();

            // Act
            parameters.StoreParams(table);

            // Assert
            Assert.Equal<string>("123", table["connection_serial"]);
        }

        [Fact]
        public void StoreTransportParams_ClientID()
        {
            // Arrange
            string target = "test123";
            TransportParams parameters = new TransportParams(new AblyRealtimeOptions() { ClientId = target });
            var table = new System.Net.WebHeaderCollection();

            // Act
            parameters.StoreParams(table);

            // Assert
            Assert.Equal<string>(target, table["client_id"]);
        }

        #region Connection

        [Fact]
        public void ConnectionPing_Calls_ConnectionManager_Ping()
        {
            // Arrange
            Mock<ITransport> mock = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackmock = new Mock<IAcknowledgementProcessor>();
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            state.Setup(c => c.State).Returns(ConnectionState.Connected);
            ConnectionManager target = new ConnectionManager(mock.Object, ackmock.Object, state.Object);

            // Act
            target.Ping(null);

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

        [Fact]
        public void ConnectionSerialUpdated_WhenProtocolMessageReceived()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Closed);
            transport.SetupProperty(c => c.Listener);
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message) { connectionSerial = 123456 };

            // Act
            transport.Object.Listener.OnTransportMessageReceived(targetMessage);

            // Assert
            Assert.Equal(123456, target.Connection.Serial);
        }

        [Fact]
        public void ConnectionSerialNotUpdated_WhenProtocolMessageReceived()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Closed);
            transport.SetupProperty(c => c.Listener);
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);
            target.Connection.Serial = 123456;
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message);

            // Act
            transport.Object.Listener.OnTransportMessageReceived(targetMessage);

            // Assert
            Assert.Equal(123456, target.Connection.Serial);
        }
        #endregion

        #region ConnectionHeartbeatRequest tests

        [Theory]
        [InlineData(ConnectionState.Closed)]
        [InlineData(ConnectionState.Closing)]
        [InlineData(ConnectionState.Connecting)]
        [InlineData(ConnectionState.Disconnected)]
        [InlineData(ConnectionState.Failed)]
        [InlineData(ConnectionState.Initialized)]
        [InlineData(ConnectionState.Suspended)]
        public void ConnectionHeartbeatRequest_FailsWhenNotConnected(ConnectionState state)
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.Setup(c => c.ConnectionState).Returns(state);
            manager.Setup(c => c.Connection).Returns(new Connection(manager.Object));
            List<Tuple<bool, ErrorInfo>> res = new List<Tuple<bool, ErrorInfo>>();
            Action<bool, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, null, callback);

            // Assert
            Assert.Equal<int>(1, res.Count);
            Assert.False(res[0].Item1);
            Assert.NotNull(res[0].Item2);
        }

        [Theory]
        [InlineData(ConnectionState.Closed)]
        [InlineData(ConnectionState.Closing)]
        [InlineData(ConnectionState.Connecting)]
        [InlineData(ConnectionState.Disconnected)]
        [InlineData(ConnectionState.Failed)]
        [InlineData(ConnectionState.Initialized)]
        [InlineData(ConnectionState.Suspended)]
        public void ConnectionHeartbeatRequest_FailsWhenNotConnected_WithNoCallback(ConnectionState state)
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
            manager.Setup(c => c.ConnectionState).Returns(ConnectionState.Connected);
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
            manager.Setup(c => c.ConnectionState).Returns(ConnectionState.Connected);
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
            connection.Setup(c => c.State).Returns(ConnectionState.Connected);
            manager.Setup(c => c.ConnectionState).Returns(ConnectionState.Connected);
            manager.Setup(c => c.Connection).Returns(connection.Object);

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, null, null);
            connection.Raise(c => c.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(ConnectionState.Closed, ConnectionState.Connected, 0, null));
        }

        [Fact]
        public void ConnectionHeartbeatRequest_SendsMessage()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<States.ICountdownTimer> timer = new Mock<States.ICountdownTimer>();
            manager.Setup(c => c.ConnectionState).Returns(ConnectionState.Connected);
            manager.Setup(c => c.Connection).Returns(new Connection(manager.Object));
            List<Tuple<bool, ErrorInfo>> res = new List<Tuple<bool, ErrorInfo>>();
            Action<bool, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));

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
            Mock<States.ICountdownTimer> timer = new Mock<States.ICountdownTimer>();
            manager.Setup(c => c.ConnectionState).Returns(ConnectionState.Connected);
            manager.Setup(c => c.Connection).Returns(new Connection(manager.Object));
            List<Tuple<bool, ErrorInfo>> res = new List<Tuple<bool, ErrorInfo>>();
            Action<bool, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, timer.Object, callback);

            // Assert
            timer.Verify(c => c.Start(It.IsAny<int>(), It.IsAny<Action>()), Times.Once());
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
            Mock<States.ICountdownTimer> timer = new Mock<States.ICountdownTimer>();
            manager.Setup(c => c.ConnectionState).Returns(ConnectionState.Connected);
            manager.Setup(c => c.Connection).Returns(new Connection(manager.Object));
            List<Tuple<bool, ErrorInfo>> res = new List<Tuple<bool, ErrorInfo>>();
            Action<bool, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));

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
            Mock<States.ICountdownTimer> timer = new Mock<States.ICountdownTimer>();
            manager.Setup(c => c.ConnectionState).Returns(ConnectionState.Connected);
            manager.Setup(c => c.Connection).Returns(new Connection(manager.Object));
            List<Tuple<bool, ErrorInfo>> res = new List<Tuple<bool, ErrorInfo>>();
            Action<bool, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, timer.Object, callback);
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));

            // Assert
            Assert.Equal<int>(1, res.Count);
            Assert.True(res[0].Item1);
            Assert.Null(res[0].Item2);
        }

        [Fact]
        public void ConnectionHeartbeatRequest_ListensForMessages_Heartbeat_StopsTimer()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<States.ICountdownTimer> timer = new Mock<States.ICountdownTimer>();
            manager.Setup(c => c.ConnectionState).Returns(ConnectionState.Connected);
            manager.Setup(c => c.Connection).Returns(new Connection(manager.Object));
            List<Tuple<bool, ErrorInfo>> res = new List<Tuple<bool, ErrorInfo>>();
            Action<bool, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));

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
            Mock<States.ICountdownTimer> timer = new Mock<States.ICountdownTimer>();
            Mock<Connection> connection = new Mock<Connection>();
            connection.Setup(c => c.State).Returns(ConnectionState.Connected);
            manager.Setup(c => c.ConnectionState).Returns(ConnectionState.Connected);
            manager.Setup(c => c.Connection).Returns(connection.Object);
            List<Tuple<bool, ErrorInfo>> res = new List<Tuple<bool, ErrorInfo>>();
            Action<bool, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, timer.Object, callback);
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));
            connection.Raise(c => c.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(ConnectionState.Closing, ConnectionState.Closed, 0, null));
            connection.Raise(c => c.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(ConnectionState.Closing, ConnectionState.Closed, 0, null));
            connection.Raise(c => c.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(ConnectionState.Closing, ConnectionState.Closed, 0, null));
            connection.Raise(c => c.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(ConnectionState.Closing, ConnectionState.Closed, 0, null));

            // Assert
            Assert.Equal<int>(1, res.Count);
        }

        [Theory]
        [InlineData(ConnectionState.Closed)]
        [InlineData(ConnectionState.Closing)]
        [InlineData(ConnectionState.Connecting)]
        [InlineData(ConnectionState.Disconnected)]
        [InlineData(ConnectionState.Failed)]
        [InlineData(ConnectionState.Initialized)]
        [InlineData(ConnectionState.Suspended)]
        public void ConnectionHeartbeatRequest_ListensForStateChanges(ConnectionState state)
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<States.ICountdownTimer> timer = new Mock<States.ICountdownTimer>();
            Mock<Connection> connection = new Mock<Connection>();
            connection.Setup(c => c.State).Returns(ConnectionState.Connected);
            manager.Setup(c => c.ConnectionState).Returns(ConnectionState.Connected);
            manager.Setup(c => c.Connection).Returns(connection.Object);
            List<Tuple<bool, ErrorInfo>> res = new List<Tuple<bool, ErrorInfo>>();
            Action<bool, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, timer.Object, callback);
            connection.Raise(c => c.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(state, state, 0, null));

            // Assert
            Assert.Equal<int>(1, res.Count);
            Assert.False(res[0].Item1);
            Assert.NotNull(res[0].Item2);
        }

        [Theory]
        [InlineData(ConnectionState.Closed)]
        [InlineData(ConnectionState.Closing)]
        [InlineData(ConnectionState.Connecting)]
        [InlineData(ConnectionState.Disconnected)]
        [InlineData(ConnectionState.Failed)]
        [InlineData(ConnectionState.Initialized)]
        [InlineData(ConnectionState.Suspended)]
        public void ConnectionHeartbeatRequest_ListensForStateChanges_StopsTimer(ConnectionState state)
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<States.ICountdownTimer> timer = new Mock<States.ICountdownTimer>();
            Mock<Connection> connection = new Mock<Connection>();
            connection.SetupGet(c => c.State).Returns(ConnectionState.Connected);
            manager.Setup(c => c.ConnectionState).Returns(ConnectionState.Connected);
            manager.Setup(c => c.Connection).Returns(connection.Object);
            Action<bool, ErrorInfo> callback = (e, err) => { };

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, timer.Object, callback);
            connection.Raise(c => c.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(state, state, 0, null));

            // Assert
            timer.Verify(c => c.Abort(), Times.Once());
        }

        [Theory]
        [InlineData(ConnectionState.Closed)]
        [InlineData(ConnectionState.Closing)]
        [InlineData(ConnectionState.Connecting)]
        [InlineData(ConnectionState.Disconnected)]
        [InlineData(ConnectionState.Failed)]
        [InlineData(ConnectionState.Initialized)]
        [InlineData(ConnectionState.Suspended)]
        public void ConnectionHeartbeatRequest_ListensForStateChanges_Unsubscribes(ConnectionState state)
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<States.ICountdownTimer> timer = new Mock<States.ICountdownTimer>();
            Mock<Connection> connection = new Mock<Connection>();
            connection.Setup(c => c.State).Returns(ConnectionState.Connected);
            manager.Setup(c => c.ConnectionState).Returns(ConnectionState.Connected);
            manager.Setup(c => c.Connection).Returns(connection.Object);
            List<Tuple<bool, ErrorInfo>> res = new List<Tuple<bool, ErrorInfo>>();
            Action<bool, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, timer.Object, callback);
            connection.Raise(c => c.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(state, state, 0, null));
            connection.Raise(c => c.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(state, state, 0, null));
            connection.Raise(c => c.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(state, state, 0, null));
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
            Mock<States.ICountdownTimer> timer = new Mock<States.ICountdownTimer>();
            Mock<Connection> connection = new Mock<Connection>();
            connection.Setup(c => c.State).Returns(ConnectionState.Connected);
            manager.Setup(c => c.ConnectionState).Returns(ConnectionState.Connected);
            manager.Setup(c => c.Connection).Returns(connection.Object);
            List<Tuple<bool, ErrorInfo>> res = new List<Tuple<bool, ErrorInfo>>();
            Action<bool, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));
            timer.Setup(c => c.Start(It.IsAny<int>(), It.IsAny<Action>())).Callback<int, Action>((c, e) => e());

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, timer.Object, callback);

            // Assert
            Assert.Equal<int>(1, res.Count);
            Assert.False(res[0].Item1);
            Assert.NotNull(res[0].Item2);
        }

        [Fact]
        public void ConnectionHeartbeatRequest_TimeOut_Unsubscribe()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<States.ICountdownTimer> timer = new Mock<States.ICountdownTimer>();
            Mock<Connection> connection = new Mock<Connection>();
            connection.Setup(c => c.State).Returns(ConnectionState.Connected);
            manager.Setup(c => c.ConnectionState).Returns(ConnectionState.Connected);
            manager.Setup(c => c.Connection).Returns(connection.Object);
            List<Tuple<bool, ErrorInfo>> res = new List<Tuple<bool, ErrorInfo>>();
            Action<bool, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));
            timer.Setup(c => c.Start(It.IsAny<int>(), It.IsAny<Action>())).Callback<int, Action>((c, e) => e());

            // Act
            ConnectionHeartbeatRequest target = ConnectionHeartbeatRequest.Execute(manager.Object, timer.Object, callback);
            connection.Raise(c => c.ConnectionStateChanged += null, new ConnectionStateChangedEventArgs(ConnectionState.Closed, ConnectionState.Closed, 0, null));
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));

            // Assert
            Assert.Equal<int>(1, res.Count);
            Assert.False(res[0].Item1);
            Assert.NotNull(res[0].Item2);
        }

        [Fact]
        public void ConnectionHeartbeatRequest_MultipleRequests()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<States.ICountdownTimer> timer = new Mock<States.ICountdownTimer>();
            manager.Setup(c => c.ConnectionState).Returns(ConnectionState.Connected);
            manager.Setup(c => c.Connection).Returns(new Connection(manager.Object));
            List<Tuple<bool, ErrorInfo>> res = new List<Tuple<bool, ErrorInfo>>();
            Action<bool, ErrorInfo> callback = (e, err) => res.Add(Tuple.Create(e, err));
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
                Assert.True(item.Item1);
                Assert.Null(item.Item2);
            }
        }

        #endregion

        [Fact]
        public void WhenRestoringConnection_UsesLastKnownConnectionSerial()
        {
            // Arrange
            long targetSerial = 1234567;
            Mock<ConnectionManager> target = new Mock<ConnectionManager>(new AblyRealtimeOptions());
            target.Object.Connection.Serial = targetSerial;
            target.Setup(c => c.CreateTransport(It.IsAny<TransportParams>())).Returns(new Mock<ITransport>().Object);

            // Act
            (target.Object as IConnectionContext).CreateTransport(false);

            // Assert
            target.Verify(c => c.CreateTransport(It.Is<TransportParams>(tp => tp.ConnectionSerial == targetSerial.ToString())), Times.Once());
        }

        [Fact]
        public void WhenRestoringConnection_UsesConnectionKey()
        {
            // Arrange
            string targetKey = "1234567";
            Mock<ConnectionManager> target = new Mock<ConnectionManager>(new AblyRealtimeOptions());
            target.Object.Connection.Key = targetKey;
            target.Setup(c => c.CreateTransport(It.IsAny<TransportParams>())).Returns(new Mock<ITransport>().Object);

            // Act
            (target.Object as IConnectionContext).CreateTransport(false);

            // Assert
            target.Verify(c => c.CreateTransport(It.Is<TransportParams>(tp => tp.ConnectionKey == targetKey.ToString())), Times.Once());
        }
    }
}
