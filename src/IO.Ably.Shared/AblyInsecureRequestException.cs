namespace IO.Ably
{
    /// <summary>
    /// Ably exception if an action cannot be performed over http.
    /// </summary>
    public class AblyInsecureRequestException : AblyException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AblyInsecureRequestException"/> class.
        /// </summary>
        public AblyInsecureRequestException()
            : base("Current action cannot be performed over http")
        {
        }
    }
}
