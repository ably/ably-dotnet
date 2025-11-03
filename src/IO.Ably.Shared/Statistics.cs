using System;
using System.Globalization;
using MessagePack;
using Newtonsoft.Json;

namespace IO.Ably
{
    /// <summary>
    /// A class encapsulating a Stats datapoint.
    /// Ably usage information, across an account or an individual app,
    /// is available as Stats records on a timeline with different granularities.
    /// This class defines the Stats type and its subtypes, giving a structured
    /// representation of service usage for a specific scope and time interval.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class Stats
    {
        /// <summary>
        /// Aggregates inbound and outbound messages.
        /// </summary>
        [Key("all")]
        public MessageTypes All { get; set; }

        /// <summary>
        /// All inbound messages.
        /// </summary>
        [Key("inbound")]
        public InboundMessageTraffic Inbound { get; set; }

        /// <summary>
        /// All outbound messages.
        /// </summary>
        [Key("outbound")]
        public OutboundMessageTraffic Outbound { get; set; }

        /// <summary>
        /// Messages persisted for later retrieval via the history API.
        /// </summary>
        [Key("persisted")]
        public MessageTypes Persisted { get; set; }

        /// <summary>
        /// Breakdown of connection stats data for different (TLS vs non-TLS) connection types.
        /// </summary>
        [Key("connections")]
        public ConnectionTypes Connections { get; set; }

        /// <summary>
        /// Breakdown of channels stats.
        /// </summary>
        [Key("channels")]
        public ResourceCount Channels { get; set; }

        /// <summary>
        /// Breakdown of API requests received via the REST API.
        /// </summary>
        [Key("apiRequests")]
        public RequestCount ApiRequests { get; set; }

        /// <summary>
        /// Breakdown of Token requests received via the REST API.
        /// </summary>
        [Key("tokenRequests")]
        public RequestCount TokenRequests { get; set; }

        /// <summary>
        /// The interval that this statistic applies to.
        /// </summary>
        [Key("intervalId")]
        [JsonProperty("intervalId")]
        public string IntervalId { get; set; }

        /// <summary>
        /// The granularity of the interval for the stat such as :day, :hour, :minute, see <see cref="StatsIntervalGranularity"/>.
        /// </summary>
        [Key("intervalGranularity")]
        [JsonProperty("intervalGranularity")]
        public StatsIntervalGranularity IntervalGranularity { get; set; }

        /// <summary>
        /// A DateTimeOffset representing the start of the interval.
        /// </summary>
        [Key("intervalTime")]
        [JsonProperty("intervalTime")]
        public DateTimeOffset IntervalTime { get; set; }

        /// <summary>
        /// IntervalId converted to a DateTimeOffset.
        /// </summary>
        [IgnoreMember]
        public DateTimeOffset Interval => DateTimeOffset.ParseExact(IntervalId, "yyyy-MM-dd:HH:mm", CultureInfo.InvariantCulture);

        /// <summary>
        /// Initializes a new instance of the <see cref="Stats"/> class.
        /// </summary>
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

    /// <summary>
    /// A breakdown of summary stats data for different (tls vs non-tls)
    /// connection types.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class ConnectionTypes
    {
        /// <summary>
        /// All connection count (includes both TLS &amp; non-TLS connections).
        /// </summary>
        [Key("all")]
        public ResourceCount All { get; set; }

        /// <summary>
        /// Non-TLS connection count (unencrypted).
        /// </summary>
        [Key("plain")]
        public ResourceCount Plain { get; set; }

        /// <summary>
        /// TLS connection count.
        /// </summary>
        [Key("tls")]
        public ResourceCount Tls { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionTypes"/> class.
        /// </summary>
        public ConnectionTypes()
        {
            All = new ResourceCount();
            Plain = new ResourceCount();
            Tls = new ResourceCount();
        }
    }

    /// <summary>
    /// MessageCount contains aggregate counts for messages and data transferred.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class MessageCount
    {
        /// <summary>
        /// Count of all message.
        /// </summary>
        [Key("count")]
        public long Count { get; set; }

        /// <summary>
        /// Total data transferred for all messages in bytes.
        /// </summary>
        [Key("data")]
        public long Data { get; set; }
    }

    /// <summary>
    /// A breakdown of summary stats data for different (message vs presence)
    /// message types.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class MessageTypes
    {
        /// <summary>
        /// All messages count (includes both presence &amp; messages).
        /// </summary>
        [Key("all")]
        public MessageCount All { get; set; }

        /// <summary>
        /// Count of channel messages.
        /// </summary>
        [Key("messages")]
        public MessageCount Messages { get; set; }

        /// <summary>
        /// Count of presence messages.
        /// </summary>
        [Key("presence")]
        public MessageCount Presence { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageTypes"/> class.
        /// </summary>
        public MessageTypes()
        {
            All = new MessageCount();
            Messages = new MessageCount();
            Presence = new MessageCount();
        }
    }

    /// <summary>
    /// A breakdown of summary stats data for traffic over various transport types.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class InboundMessageTraffic
    {
        /// <summary>
        /// All messages count (includes realtime, rest and webhook messages).
        /// </summary>
        [Key("all")]
        public MessageTypes All { get; set; }

        /// <summary>
        /// Count of messages transferred over a realtime transport such as WebSockets.
        /// </summary>
        [Key("realtime")]
        public MessageTypes Realtime { get; set; }

        /// <summary>
        /// Count of messages transferred using REST.
        /// </summary>
        [Key("rest")]
        public MessageTypes Rest { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InboundMessageTraffic"/> class.
        /// </summary>
        public InboundMessageTraffic()
        {
            All = new MessageTypes();
            Realtime = new MessageTypes();
            Rest = new MessageTypes();
        }
    }

    /// <summary>
    /// A breakdown of summary stats data for traffic over various transport types.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class OutboundMessageTraffic
    {
        /// <summary>
        /// All messages count (includes realtime, rest and webhook messages).
        /// </summary>
        [Key("all")]
        public MessageTypes All { get; set; }

        /// <summary>
        /// Count of messages transferred over a realtime transport such as WebSockets.
        /// </summary>
        [Key("realtime")]
        public MessageTypes Realtime { get; set; }

        /// <summary>
        /// Count of messages transferred using REST.
        /// </summary>
        [Key("rest")]
        public MessageTypes Rest { get; set; }

        /// <summary>
        /// Count of messages delivered using WebHooks.
        /// </summary>
        [Key("webhook")]
        public MessageTypes Webhook { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OutboundMessageTraffic"/> class.
        /// </summary>
        public OutboundMessageTraffic()
        {
            All = new MessageTypes();
            Realtime = new MessageTypes();
            Rest = new MessageTypes();
            Webhook = new MessageTypes();
        }
    }

    /// <summary>
    /// RequestCount contains aggregate counts for requests made.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class RequestCount
    {
        /// <summary>
        /// Requests succeeded.
        /// </summary>
        [Key("succeeded")]
        public long Succeeded { get; set; }

        /// <summary>
        /// Requests failed.
        /// </summary>
        [Key("failed")]
        public long Failed { get; set; }

        /// <summary>
        /// Requests refused typically as a result of permissions or a limit being exceeded.
        /// </summary>
        [Key("refused")]
        public long Refused { get; set; }
    }

    /// <summary>
    /// Aggregate data for usage of a resource in a specific scope.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class ResourceCount
    {
        /// <summary>
        /// Total resources of this type opened.
        /// </summary>
        [Key("opened")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double Opened { get; set; }

        /// <summary>
        /// Peak resources of this type used for this period.
        /// </summary>
        [Key("peak")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double Peak { get; set; }

        /// <summary>
        /// Average resources of this type used for this period.
        /// </summary>
        [Key("mean")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double Mean { get; set; }

        /// <summary>
        /// Minimum total resources of this type used for this period.
        /// </summary>
        [Key("min")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double Min { get; set; }

        /// <summary>
        /// Resource requests refused within this period.
        /// </summary>
        [Key("refused")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double Refused { get; set; }
    }
}
