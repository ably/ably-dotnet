using MsgPack.Serialization;
using Newtonsoft.Json;

namespace IO.Ably.Rest
{
#pragma warning disable SA1307
#pragma warning disable SA1600

    [MessagePackObject]
    public class ChannelDetails
    {
        /// <summary>
        /// The required name of the channel including any qualifier, if any.
        /// </summary>
        [Key(0)]
        [JsonProperty("channelId")]
        public string ChannelId { get; set; }

        /// <summary>
        /// The status and occupancy stats for the channel.
        /// </summary>
        [Key(1)]
        [JsonProperty("status")]
        public ChannelStatus Status { get; set; }
    }

    [MessagePackObject]
    public class ChannelStatus
    {
        /// <summary>
        /// Indicates whether the channel that is the subject of the event is active.
        /// </summary>
        [Key(0)]
        [JsonProperty("isActive")]
        public bool IsActive { get; set; }

        /// <summary>
        /// Metadata relating to the occupants of the channel.
        /// </summary>
        [Key(1)]
        [JsonProperty("occupancy")]
        public ChannelOccupancy Occupancy { get; set; }
    }

    /// <summary>
    /// Metadata relating to the occupants of the channel.
    /// </summary>
    [MessagePackObject]
    public class ChannelOccupancy
    {
        [Key(0)]
        [JsonProperty("metrics")]
        public ChannelMetrics Metrics { get; set; }
    }

    [MessagePackObject]
    public class ChannelMetrics
    {
        /// <summary>
        /// The number of connections.
        /// </summary>
        [Key(0)]
        [JsonProperty("connections")]
        public int Connections { get; set; }

        /// <summary>
        /// The number of connections attached to the channel that are authorised to publish.
        /// </summary>
        [Key(1)]
        [JsonProperty("publishers")]
        public int Publishers { get; set; }

        /// <summary>
        /// The number of connections attached that are authorised to subscribe to messages.
        /// </summary>
        [Key(2)]
        [JsonProperty("subscribers")]
        public int Subscribers { get; set; }

        /// <summary>
        /// The number of connections that are authorised to enter members into the presence channel.
        /// </summary>
        [Key(3)]
        [JsonProperty("presenceConnections")]
        public int PresenceConnections { get; set; }

        /// <summary>
        /// The number of members currently entered into the presence channel.
        /// </summary>
        [Key(4)]
        [JsonProperty("presenceMembers")]
        public int PresenceMembers { get; set; }

        /// <summary>
        /// The number of connections that are authorised to subscribe to presence messages.
        /// </summary>
        [Key(5)]
        [JsonProperty("presenceSubscribers")]
        public int PresenceSubscribers { get; set; }
    }
}
