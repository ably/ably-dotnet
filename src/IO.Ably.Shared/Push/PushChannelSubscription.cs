namespace IO.Ably.Push
{
    /// <summary>
    /// Represents a push channel subscription.
    /// </summary>
    public class PushChannelSubscription
    {
        /// <summary>
        /// Name of the channel.
        /// </summary>
        public string Channel { get; }

        /// <summary>
        /// Device id attached to the subscription.
        /// </summary>
        public string DeviceId { get; }

        /// <summary>
        /// Client id attached to the channel.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Factory method for creating a PushChannelSubscription for a deviceId.
        /// </summary>
        /// <param name="channel">Name of the channel.</param>
        /// <param name="deviceId">Device id.</param>
        /// <returns>Returns an instance of PushChannelSubscription.</returns>
        public static PushChannelSubscription ForDevice(string channel, string deviceId)
        {
            return new PushChannelSubscription(channel, deviceId, null);
        }

        /// <summary>
        /// Factory method for creating a PushChannelSubscription for a clientId.
        /// </summary>
        /// <param name="channel">Name of the channel.</param>
        /// <param name="clientId">Client id.</param>
        /// <returns>Returns an instance of PushChannelSubscription.</returns>
        public static PushChannelSubscription ForClientId(string channel, string clientId)
        {
            return new PushChannelSubscription(channel, null, clientId);
        }

        private PushChannelSubscription(string channel, string deviceId, string clientId)
        {
            Channel = channel;
            DeviceId = deviceId;
            ClientId = clientId;
        }
    }
}
