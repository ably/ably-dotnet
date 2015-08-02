using Xunit;

namespace Ably.Tests
{
    public class AuthenticationTests
    {
        private static readonly string Debug_Key = "1iZPfA.BjcI_g:wpNhw5RCw6rDjisl";

        [Fact]
        public void New_Realtime_HasAuth()
        {
            AblyRealtime realtime = new AblyRealtime(Debug_Key);
            Assert.NotNull(realtime.Auth);
        }
    }
}
