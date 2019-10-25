using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably;
using IO.Ably.Realtime;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;

namespace IO.Ably.Transport
{
    internal class ConnectionHeartbeatHandler
    {
        private readonly ConnectionManager _manager;
        private readonly ILogger _logger;

        private DateTimeOffset Now => _manager.Now();

        public ConnectionHeartbeatHandler(ConnectionManager manager, ILogger logger)
        {
            _manager = manager;
            _logger = logger;
        }

        public static bool CanHandleMessage(ProtocolMessage message)
        {
            return message.Action == ProtocolMessage.MessageAction.Heartbeat;
        }

        public Task<bool> OnMessageReceived(ProtocolMessage message, RealtimeState state)
        {
            var canHandle = CanHandleMessage(message);
            if (canHandle && message.Id.IsNotEmpty())
            {
                var pingRequest = state.PingRequests.FirstOrDefault(x => x.Id.EqualsTo(message.Id));

                if (pingRequest != null)
                {
                    state.PingRequests.Remove(pingRequest);
                    TryCallback(pingRequest.Callback, GetElapsedTime(pingRequest), null);
                }
            }

            return Task.FromResult(canHandle);
        }

        private void TryCallback(Action<TimeSpan?, ErrorInfo> action, TimeSpan? elapsed, ErrorInfo error)
        {
            try
            {
                action.Invoke(elapsed, error);
            }
            catch (Exception e)
            {
                _logger.Error("Error executing callback for Ping request", e);
            }
        }

        private TimeSpan? GetElapsedTime(PingRequest pingRequest)
        {
            return Now - pingRequest.Created;
        }
    }
}
