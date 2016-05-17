using System;
using System.Threading.Tasks;
using IO.Ably.Transport;

namespace IO.Ably.Tests.Realtime
{
    public class FakeTransportFactory : ITransportFactory
    {
        public FakeTransport LastCreatedTransport { get; set; }
        public Action<FakeTransport> initialiseFakeTransport = delegate { };


        public ITransport CreateTransport(TransportParams parameters)
        {
            LastCreatedTransport = new FakeTransport(parameters);
            initialiseFakeTransport(LastCreatedTransport);
            return LastCreatedTransport;
        }
    }
}