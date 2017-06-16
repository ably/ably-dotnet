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
                Key = "key goes here",
                ClientId = "stan",
                Tls = true, 
                UseBinaryProtocol = false
            };
            return new AblyRealtime(options);
        }
    }
}