namespace IO.Ably.Push
{
    /// <summary>
    /// PushChannel is a convenience class that facilitates push related actions,
    /// like subscribing and unsubscribing to push notification,
    /// narrowed to a specific channel.
    /// </summary>
    public class PushChannel
    {
        private readonly AblyRest _rest;

        internal string ChannelName { get; }

        /// <summary>
        /// Create a new instance of PushChannel.
        /// </summary>
        /// <param name="channelName">Name of the channel.</param>
        /// <param name="rest"><see cref="AblyRest"/> client.</param>
        internal PushChannel(string channelName, AblyRest rest)
        {
            ChannelName = channelName;
            _rest = rest;
        }
    }
}
