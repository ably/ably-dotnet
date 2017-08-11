using System;

namespace IO.Ably.ConsoleTest
{
    class Realtime
    {
        public static AblyRealtime Test()
        {
            var options = new ClientOptions("lNj80Q.iGyVcQ:2QKX7FFASfX-7H9H")
            {
                ClientId = "stan",
                UseBinaryProtocol = false
            };
            return new AblyRealtime(options);
        }
    }
}