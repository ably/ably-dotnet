using IO.Ably.Realtime;
using IO.Ably.Transport;

namespace IO.Ably.Tests.Infrastructure
{
    public class TestTransportFactory : ITransportFactory
    {
        public ITransport CreateTransport(TransportParams parameters)
        {
            var factory = new WebSocketTransport.WebSocketTransportFactory();
            return new TestTransportWrapper(factory.CreateTransport(parameters), parameters.UseBinaryProtocol ? Protocol.MsgPack : Protocol.Json);
        }
    }
}