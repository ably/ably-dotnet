namespace IO.Ably
{
    /// <summary>Level of a log message.</summary>
    public enum LogLevel : byte
    {
        /// <summary>
        /// Verbose setting. Logs everything.
        /// </summary>
        Debug = 0,

        /// <summary>
        /// Warning setting. Logs clues that something is not 100% right.
        /// </summary>
        Warning,

        /// <summary>
        /// Error setting. Logs errors
        /// </summary>
        Error,

        /// <summary>
        /// None setting. No logs produced
        /// </summary>
        None = 99
    }
}
