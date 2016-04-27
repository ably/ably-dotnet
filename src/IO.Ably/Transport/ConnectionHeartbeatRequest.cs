using System;
using System.Net;
using IO.Ably.Realtime;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;

namespace IO.Ably.Transport
{
    internal class ConnectionHeartbeatRequest
    {
        private const int HeartbeatTimeout = 5*1000; //TODO: Make sure it comes from ClientOptions

        private static readonly ErrorInfo DefaultError = new ErrorInfo("Unable to ping service; not connected", 40000,
            HttpStatusCode.BadRequest);

        private Action<bool, ErrorInfo> _callback;
        private IConnectionManager _manager;
        private ICountdownTimer _timer;

        public static bool CanHandleMessage(ProtocolMessage message)
        {
            return message.action == ProtocolMessage.MessageAction.Heartbeat;
        }

        public static ConnectionHeartbeatRequest Execute(IConnectionManager manager, Action<bool, ErrorInfo> callback)
        {
            return Execute(manager, new CountdownTimer(), callback);
        }

        public static ConnectionHeartbeatRequest Execute(IConnectionManager manager, ICountdownTimer timer,
            Action<bool, ErrorInfo> callback)
        {
            var request = new ConnectionHeartbeatRequest();

            if (manager.Connection.State != ConnectionStateType.Connected)
            {
                callback?.Invoke(false, DefaultError);

                return request;
            }

            if (callback != null)
            {
                request._manager = manager;
                manager.MessageReceived += request.OnMessageReceived;
                manager.Connection.ConnectionStateChanged += request.OnConnectionStateChanged;
                request._timer = timer;
                request._callback = callback;
            }

            manager.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat), null);
            if (callback != null)
            {
                request._timer.Start(HeartbeatTimeout, request.OnTimeout);
            }

            return request;
        }

        private void OnMessageReceived(ProtocolMessage message)
        {
            if (CanHandleMessage(message))
            {
                FinishRequest(true, null);
            }
        }

        private void OnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (e.CurrentState != ConnectionStateType.Connected)
            {
                FinishRequest(false, DefaultError);
            }
        }

        private void OnTimeout()
        {
            FinishRequest(false, DefaultError);
        }

        private void FinishRequest(bool result, ErrorInfo error)
        {
            _manager.MessageReceived -= OnMessageReceived;
            _manager.Connection.ConnectionStateChanged -= OnConnectionStateChanged;
            _timer.Abort();

            if (_callback != null)
            {
                _callback(result, error);
            }

            _callback = null;
            _manager = null;
            _timer = null;
        }
    }
}