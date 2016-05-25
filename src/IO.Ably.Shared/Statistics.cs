using System;
using System.Globalization;
using IO.Ably.CustomSerialisers;
using Newtonsoft.Json;

namespace IO.Ably
{
    public class Stats
    {
        public MessageTypes All { get; set; }
        public InboundMessageTraffic Inbound { get; set; }
        public OutboundMessageTraffic Outbound { get; set; }
        public MessageTypes Persisted { get; set; }
        public ConnectionTypes Connections { get; set; }
        public ResourceCount Channels { get; set; }
        public RequestCount ApiRequests { get; set; }
        public RequestCount TokenRequests { get; set; }

        [JsonProperty("intervalId")]
        public string IntervalId { get; set; }

        public DateTimeOffset Interval => DateTimeOffset.ParseExact(IntervalId, "yyyy-MM-dd:HH:mm", CultureInfo.InvariantCulture);

        public Stats()
        {
            All = new MessageTypes();
            Inbound = new InboundMessageTraffic();
            Outbound = new OutboundMessageTraffic();
            Persisted = new MessageTypes();
            Connections = new ConnectionTypes();
            Channels = new ResourceCount();
            ApiRequests = new RequestCount();
            TokenRequests = new RequestCount();
        }
    }

    public class ConnectionTypes
    {
        public ResourceCount All { get; set; }
        public ResourceCount Plain { get; set; }
        public ResourceCount Tls { get; set; }

        public ConnectionTypes()
        {
            All = new ResourceCount();
            Plain = new ResourceCount();
            Tls = new ResourceCount();
        }
    }

    public class MessageCount
    {
        public long Count { get; set; }
        public long Data { get; set; }
    }

    /**
     * A breakdown of summary stats data for different (message vs presence)
     * message types.
     */
    public class MessageTypes
    {
        public MessageCount All { get; set; }
        public MessageCount Messages { get; set; }
        public MessageCount Presence { get; set; }

        public MessageTypes()
        {
            All = new MessageCount();
            Messages = new MessageCount();
            Presence = new MessageCount();
        }
    }

    /**
     * A breakdown of summary stats data for traffic over various transport types.
     */
    public class InboundMessageTraffic
    {
        public MessageTypes All { get; set; }
        public MessageTypes Realtime { get; set; }
        public MessageTypes Rest { get; set; }

        public InboundMessageTraffic()
        {
            All = new MessageTypes();
            Realtime = new MessageTypes();
            Rest = new MessageTypes();
        }
    }

    public class OutboundMessageTraffic
    {
        public MessageTypes All { get; set; }
        public MessageTypes Realtime { get; set; }
        public MessageTypes Rest { get; set; }
        public MessageTypes Webhook { get; set; }

        public OutboundMessageTraffic()
        {
            All = new MessageTypes();
            Realtime = new MessageTypes();
            Rest = new MessageTypes();
            Webhook = new MessageTypes();
        }
    }

    public class RequestCount
    {
        public long Succeeded { get; set; }
        public long Failed { get; set; }
        public long Refused { get; set; }
    }

    public class ResourceCount
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double Opened { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double Peak { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double Mean { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double Min { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double Refused { get; set; }
    }
}
