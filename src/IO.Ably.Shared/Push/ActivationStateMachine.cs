namespace IO.Ably.Push
{
    internal partial class ActivationStateMachine
    {
        private readonly AblyRest _restClient;
        private readonly ILogger _logger;

        internal ActivationStateMachine(AblyRest restClient, ILogger logger)
        {
            _restClient = restClient;
            _logger = logger;
        }
    }
}
