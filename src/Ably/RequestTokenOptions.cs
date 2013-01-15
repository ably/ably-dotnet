using System;
using System.Collections.Generic;
using System.Linq;

namespace Ably
{
    public class RequestTokenParams
    {
        public String Id { get; set;}
        public TimeSpan? Ttl { get; set; }
        public String Capability { get; set; }
        public String ClientId { get; set; }
        public long TimeStamp { get; set; }
        public String Nonce { get; set; }
        public String Mac { get; set; }
    }
}
