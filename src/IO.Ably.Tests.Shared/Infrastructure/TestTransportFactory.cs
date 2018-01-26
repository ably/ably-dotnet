using System;
using IO.Ably.Transport;

namespace IO.Ably.Tests.Infrastructure
{
    public class TestTransportFactory : ITransportFactory
    {
        internal Action<TestTransportWrapper> OnTransportCreated = delegate { };

        public ITransport CreateTransport(TransportParams parameters)
        {
            var factory = IoC.TransportFactory;
            var transport
                = new TestTransportWrapper(factory.CreateTransport(parameters), parameters.UseBinaryProtocol ? Defaults.Protocol : Protocol.Json);
            OnTransportCreated(transport);
            return transport;
        }
    }
}