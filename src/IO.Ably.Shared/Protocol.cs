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
}
