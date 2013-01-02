using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Ably
{
    public class RequestTokenOptions
    {
        public string ClientId { get; set; }
        public string Capability { get; set; }
        public TimeSpan? Expires { get; set; }

    }
}
