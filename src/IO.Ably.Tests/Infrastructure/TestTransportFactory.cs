using IO.Ably.Realtime;
using IO.Ably.Transport;

namespace IO.Ably.Tests.Infrastructure
{
    public class TestTransportFactory : ITransportFactory
    {
        public ITransport CreateTransport(TransportParams parameters)
        {
            var factory = new MsWebSocketTransport.TransportFactory();
            return new TestTransportWrapper(factory.CreateTransport(parameters), parameters.UseBinaryProtocol ? Protocol.MsgPack : Protocol.Json);
        }
    }
}