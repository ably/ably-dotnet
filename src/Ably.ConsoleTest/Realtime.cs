using System;
using System.Threading.Tasks;

namespace IO.Ably.ConsoleTest
{
    class Realtime
    {
        public static AblyRealtime Test()
        {
            var options = new ClientOptions()
            {
                AuthUrl = new Uri("https://www.ably.io/ably-auth/token-request/demos"),
                ClientId = "stan",
                Tls = false,
            };
            return new AblyRealtime(options);
        }
    }
}