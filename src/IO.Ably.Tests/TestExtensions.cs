using IO.Ably.Tests.Infrastructure;

namespace IO.Ably.Tests
{
    public static class TestExtensions
    {
        internal static TestTransportWrapper GetTestTransport(this IRealtimeClient client)
        {
            return ((AblyRealtime)client).ConnectionManager.Transport as TestTransportWrapper;
        }
    }
}