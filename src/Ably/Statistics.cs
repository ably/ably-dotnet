using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ably
{
    public class Stats
    {
        Traffic Published { get; set; }
        Traffic DeliveredAll { get; set; }
        Traffic DeliveredRest { get; set; }
        Traffic DeliveredRealTime { get; set; }
        Traffic DeliveredPost { get; set; }
    }

    public class ChannelStats : Stats
    {
        public string ChannelName { get; set; }
    }

    public class Traffic
    {
        public long MessageCount { get; set; }
        public double MessageSize { get; set; }
    }
}
