namespace IO.Ably.Push
{
    /// <summary>
    /// Push Admin APIs.
    /// </summary>
    public class PushAdmin
    {
        private readonly AblyRest _restClient;
        private readonly ILogger _logger;

        internal PushAdmin(AblyRest restClient, ILogger logger)
        {
            _restClient = restClient;
            _logger = logger;
        }
    }
}
