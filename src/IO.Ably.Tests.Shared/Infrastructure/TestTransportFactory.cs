using System;
using IO.Ably.Transport;
using IO.Ably.Types;

namespace IO.Ably.Tests.Infrastructure
{
    public class TestTransportFactory : ITransportFactory
    {
        internal Action<TestTransportWrapper> OnTransportCreated = delegate { };

        internal Action<ProtocolMessage> OnMessageSent = delegate { };

        internal Action<ProtocolMessage> BeforeDataProcessed;
        internal Action<ProtocolMessage> AfterDataReceived;

        public ITransport CreateTransport(TransportParams parameters)
        {
            var factory = IoC.TransportFactory;
            var transport
                = new TestTransportWrapper(factory.CreateTransport(parameters), parameters.UseBinaryProtocol ? Defaults.Protocol : Protocol.Json);
            transport.AfterDataReceived = AfterDataReceived;
            transport.BeforeDataProcessed = BeforeDataProcessed;
            OnTransportCreated(transport);
            transport.MessageSent = OnMessageSent;
            return transport;
        }
    }
}
