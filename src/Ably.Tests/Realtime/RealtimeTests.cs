using Ably.Realtime;
using Xunit;

namespace Ably.Tests.Realtime
{
    public class RealtimeTests
    {
        private static readonly string Debug_Key = "123:456";

        [Fact]
        public void New_Realtime_HasConnection()
        {
            AblyRealtime realtime = new AblyRealtime(Debug_Key);
            Assert.NotNull(realtime.Connection);
        }

        [Fact]
        public void New_Realtime_HasChannels()
        {
            AblyRealtime realtime = new AblyRealtime(Debug_Key);
            Assert.NotNull(realtime.Channels);
        }

        [Fact]
        public void New_Realtime_HasInitializedConnection()
        {
            AblyRealtime realtime = new AblyRealtime(Debug_Key);
            Assert.Equal<ConnectionState>(ConnectionState.Initialized, realtime.Connection.State);
        }
    }
}
