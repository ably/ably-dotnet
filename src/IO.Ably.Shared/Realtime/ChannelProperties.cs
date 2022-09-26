namespace IO.Ably.Realtime
{
    /// <summary>
    /// Channel properties.
    /// </summary>
    public class ChannelProperties
    {
        /// <summary>
        /// contains the last channelSerial received in an ATTACHED ProtocolMessage for the channel, see CP2a, RTL15a.
        /// </summary>
        public string AttachSerial { get; internal set; }
    }
}
