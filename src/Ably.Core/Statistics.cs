using System;
using IO.Ably.CustomSerialisers;
using Newtonsoft.Json;

namespace IO.Ably
{
    public class Stats
    {
        public MessageTypes All { get; set; }
        public MessageTraffic Inbound { get; set; }
        public MessageTraffic Outbound { get; set; }
        public MessageTypes Persisted { get; set; }
        public ConnectionTypes Connections { get; set; }
        public ResourceCount Channels { get; set; }
        public RequestCount ApiRequests { get; set; }
        public RequestCount TokenRequests { get; set; }
        [JsonProperty("intervalId")]
        [JsonConverter(typeof(StatsJsonDateConverter))]
        public DateTime Interval { get; set; }

        public Stats()
        {
            All = new MessageTypes();
            Inbound = new MessageTraffic();
            Outbound = new MessageTraffic();
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
        public double Count { get; set; }
        public double Data { get; set; }
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
    public class MessageTraffic
    {
        public MessageTypes All { get; set; }
        public MessageTypes Realtime { get; set; }
        public MessageTypes Rest { get; set; }
        public MessageTypes Push { get; set; }
        public MessageTypes HttpStream { get; set; }

        public MessageTraffic()
        {
            All = new MessageTypes();
            Realtime = new MessageTypes();
            Rest = new MessageTypes();
            Push = new MessageTypes();
            HttpStream = new MessageTypes();
        }
    }

    public class RequestCount
    {
        public double Succeeded { get; set; }
        public double Failed { get; set; }
        public double Refused { get; set; }
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
