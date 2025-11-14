namespace IO.Ably
{
    /// <summary>
    /// Protocol used.
    /// </summary>
    public enum Protocol
    {
        /// <summary>
        /// MessagePack binary protocol (default for better performance).
        /// </summary>
        MsgPack = 0,

        /// <summary>
        /// JSON text protocol.
        /// </summary>
        Json = 1
    }

    /// <summary>
    /// Extension methods for Protocol enum.
    /// </summary>
    public static class ProtocolExtensions
    {
        /// <summary>
        /// Determines whether the specified protocol is binary (MsgPack).
        /// </summary>
        /// <param name="protocol">The protocol to check.</param>
        /// <returns>true if the protocol is MsgPack; otherwise, false.</returns>
        public static bool IsBinary(this Protocol protocol)
        {
            return protocol == Protocol.MsgPack;
        }
    }
}
