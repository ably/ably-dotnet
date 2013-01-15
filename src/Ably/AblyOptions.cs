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
        public bool Encrypted { get; set; }
        public bool UseTextProtocol { get; set; }

        public AblyOptions()
        {
            Encrypted = true;
            UseTextProtocol = true;
        }
    }
}
