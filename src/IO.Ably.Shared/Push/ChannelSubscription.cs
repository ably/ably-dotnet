namespace IO.Ably.Push
{
    /// <summary>
    /// Represents a push channel subscription.
    /// </summary>
    public class ChannelSubscription
    {
        /// <summary>
        /// Name of the channel.
        /// </summary>
        public string Channel { get; private set; }

        /// <summary>
        /// Device id attached to the subscription.
        /// </summary>
        public string DeviceId { get; private set; }

        /// <summary>
        /// Client id attached to the channel.
        /// </summary>
        public string ClientId { get; private set; }

        /// <summary>
        /// Factory method for creating a ChannelSubscription for a deviceId.
        /// </summary>
        /// <param name="channel">Name of the channel.</param>
        /// <param name="deviceId">Device id.</param>
        /// <returns>Returns an instance of ChannelSubscription.</returns>
        public static ChannelSubscription ForDevice(string channel, string deviceId)
        {
            return new ChannelSubscription(channel, deviceId, null);
        }

        /// <summary>
        /// Factory method for creating a ChannelSubscription for a clientId.
        /// </summary>
        /// <param name="channel">Name of the channel.</param>
        /// <param name="clientId">Client id.</param>
        /// <returns>Returns an instance of ChannelSubscription.</returns>
        public static ChannelSubscription ForClientId(string channel, string clientId)
        {
            return new ChannelSubscription(channel, null, clientId);
        }

        private ChannelSubscription(string channel, string deviceId, string clientId)
        {
            Channel = channel;
            DeviceId = deviceId;
            ClientId = clientId;
        }
    }
}
