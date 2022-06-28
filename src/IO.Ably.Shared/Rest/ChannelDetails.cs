namespace IO.Ably.Rest
{
#pragma warning disable SA1307
#pragma warning disable SA1600

    public class ChannelDetails
    {
        /// <summary>
        /// The required name of the channel including any qualifier, if any.
        /// </summary>
        public string channelId;

        /// <summary>
        /// The status and occupancy stats for the channel.
        /// </summary>
        public ChannelStatus status;
    }

    public class ChannelStatus
    {
        /// <summary>
        /// Indicates whether the channel that is the subject of the event is active.
        /// </summary>
        public bool isActive;

        /// <summary>
        /// Metadata relating to the occupants of the channel.
        /// </summary>
        public ChannelOccupancy occupancy;
    }

    /// <summary>
    /// Metadata relating to the occupants of the channel.
    /// </summary>
    public class ChannelOccupancy
    {
        public ChannelMetrics metrics;
    }

    public class ChannelMetrics
    {
        /// <summary>
        /// The number of connections.
        /// </summary>
        public int connections;

        /// <summary>
        /// The number of connections attached to the channel that are authorised to publish.
        /// </summary>
        public int publishers;

        /// <summary>
        /// The number of connections attached that are authorised to subscribe to messages.
        /// </summary>
        public int subscribers;

        /// <summary>
        /// The number of connections that are authorised to enter members into the presence channel.
        /// </summary>
        public int presenceConnections;

        /// <summary>
        /// The number of members currently entered into the presence channel.
        /// </summary>
        public int presenceMembers;

        /// <summary>
        /// The number of connections that are authorised to subscribe to presence messages.
        /// </summary>
        public int presenceSubscribers;
    }
}
