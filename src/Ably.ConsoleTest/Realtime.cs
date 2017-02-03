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
                Tls = true,
                UseBinaryProtocol = true,
                LogLevel = LogLevel.Debug,
                LogHander = new MyLogger()
            };
            return new AblyRealtime(options);
        }
    }
}