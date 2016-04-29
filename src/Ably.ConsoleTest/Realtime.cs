using System;
using System.Threading.Tasks;

namespace IO.Ably.ConsoleTest
{
    class Realtime
    {
        public static void Test()
        {
            var options = new ClientOptions()
            {
                AuthUrl = new Uri("https://www.ably.io/ably-auth/token-request/demos"),
                ClientId = "stan",
                Tls = false,
            };
            var realtime = new AblyRealtime( options );
            realtime.Connect();
        }
    }
}