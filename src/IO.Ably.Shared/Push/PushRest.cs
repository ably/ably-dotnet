using IO.Ably.Infrastructure;

namespace IO.Ably.Push
{
    /// <summary>
    /// Push Apis for Rest clients.
    /// </summary>
    public class PushRest
    {
        private readonly AblyRest _rest;
        private readonly ILogger _logger;

        internal PushRest(AblyRest rest, ILogger logger)
        {
            _rest = rest;
            _logger = logger;
            Admin = new PushAdmin(rest, logger);
        }

        /// <summary>
        /// Admin APIs for Push notifications.
        /// </summary>
        public PushAdmin Admin { get; }
    }
}
