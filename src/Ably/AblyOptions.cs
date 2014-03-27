using System;
using System.Collections.Generic;
using System.Linq;

namespace Ably
{
    public class AblyOptions : AuthOptions
    {
        public string AppId { get; set; }
        public string ClientId { get; set; }

        public string Host { get; set; }
        public int? Port { get; set; }
        public bool Tls { get; set; }
        public bool UseTextProtocol { get; set; }
        public ChannelOptions ChannelDefaults { get; set; }
        public bool EchoMessages { get; set; }
        public string Recover { get; set; }
        public int? TlsPort { get; set; }
        public bool QueueMessages { get; set; }

        public AblyOptions()
        {
            Tls = true;
            QueueMessages = true;
            UseTextProtocol = true;
            EchoMessages = false;
            ChannelDefaults = new ChannelOptions();
        }
    }
}
