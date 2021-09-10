namespace IO.Ably.Push
{
    /// <summary>
    /// PushChannel is a convenience class that facilitates push related actions,
    /// like subscribing and unsubscribing to push notification,
    /// narrowed down to a specific channel.
    /// </summary>
    public class PushChannel
    {
        private readonly string _channelName;
        private readonly AblyRest _rest;

        /// <summary>
        /// Create a new instance of PushChannel.
        /// </summary>
        /// <param name="channelName">Name of the channel.</param>
        /// <param name="rest"><see cref="AblyRest"/> client.</param>
        internal PushChannel(string channelName, AblyRest rest)
        {
            _channelName = channelName;
            _rest = rest;
        }
    }
}
