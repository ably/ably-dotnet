namespace IO.Ably.Realtime
{
    /// <summary>A series of connection states.</summary>
    public enum ConnectionEvent
    {
        /*
         The values assigned to each enum should correspond with those assigned in ConnectionState.
         */
        Initialized = 0,
        Connecting = 1,
        Connected = 2,
        Disconnected = 3,
        Suspended = 4,
        Closing = 5,
        Closed = 6,
        Failed = 7,
        Update = 8
    }
}
