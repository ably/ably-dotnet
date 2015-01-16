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
        public bool UseBinaryProtocol { get; set; }
        public ChannelOptions ChannelDefaults { get; set; }
        public bool EchoMessages { get; set; }
        public string Recover { get; set; }
        public int? TlsPort { get; set; }
        public bool QueueMessages { get; set; }
        public Protocol? Protocol { get; set; }
        public AblyEnvironment? Environment { get; set; }

        public AblyOptions()
        {
            Tls = true;
            QueueMessages = true;
            UseBinaryProtocol = true;
            EchoMessages = false;
            ChannelDefaults = new ChannelOptions();
        }
    }

    public enum AblyEnvironment
    {
        Live, 
        Uat,
        Sandbox
    }
}
