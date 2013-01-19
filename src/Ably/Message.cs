using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ably
{
    public class Message
    {
        public string Name { get; set; }
        public string ChannelId { get; set; }
        public object Data { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
    }
}
