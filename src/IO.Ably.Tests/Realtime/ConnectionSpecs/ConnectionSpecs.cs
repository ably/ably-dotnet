using System;
using System.Threading.Tasks;
using IO.Ably.Types;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    public static class AblyRealtimeTestExtensions
    {
        public static Task FakeProtocolMessageReceived(this AblyRealtime client, ProtocolMessage message)
        {
            return client.ConnectionManager.OnTransportMessageReceived(message);
        }

        public static Task FakeMessageReceived(this AblyRealtime client, Message message, string channel = null)
        {
            return
                client.ConnectionManager.OnTransportMessageReceived(
                    new ProtocolMessage(ProtocolMessage.MessageAction.Message) {messages = new[] {message}, channel = channel});
        }
    }

    public class ConnectionSpecsBase : AblyRealtimeSpecs
    {
        protected FakeTransportFactory _fakeTransportFactory;
        protected FakeTransport LastCreatedTransport => _fakeTransportFactory.LastCreatedTransport;

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
            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected)
            {
                connectionDetails = new ConnectionDetails() { connectionKey = "connectionKey"},
                connectionId = "1",
                connectionSerial = 100
            });
            return client;
        }

        public ConnectionSpecsBase(ITestOutputHelper output) : base(output)
        {
            _fakeTransportFactory = new FakeTransportFactory();
        }
    }
}
