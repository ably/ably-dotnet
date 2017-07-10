using System;
using IO.Ably.Tests.Infrastructure;

namespace IO.Ably.Tests
{
    public static class TestExtensions
    {
        internal static TestTransportWrapper GetTestTransport(this IRealtimeClient client)
        {
            return ((AblyRealtime)client).ConnectionManager.Transport as TestTransportWrapper;
        }

        internal static void SetOnTransportCreated(this IRealtimeClient client, Action<TestTransportWrapper> onCreated)
        {
            var factory = ((AblyRealtime) client).Options.TransportFactory as TestTransportFactory;
            factory.OnTransportCreated = onCreated;
        }
    }
}