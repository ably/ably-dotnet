﻿using System;
using System.Threading.Tasks;

namespace IO.Ably.ConsoleTest
{
    class Realtime
    {
        public static async Task Test()
        {
            var options = new ClientOptions()
            {
                AuthUrl = new Uri("https://www.ably.io/ably-auth/token-request/demos"),
                ClientId = "stan",
                Tls = false,
            };
            var realtime = new AblyRealtime( options );
            await realtime.Connect();
        }
    }
}