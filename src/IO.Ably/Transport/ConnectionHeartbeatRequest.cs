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

        private Action<DateTimeOffset?, ErrorInfo> _callback;
        private IConnectionManager _manager;
        private ICountdownTimer _timer;
        private bool _finished;
        private object _syncLock = new object();


        public static bool CanHandleMessage(ProtocolMessage message)
        {
            return message.action == ProtocolMessage.MessageAction.Heartbeat;
        }

        public static ConnectionHeartbeatRequest Execute(IConnectionManager manager, Action<DateTimeOffset?, ErrorInfo> callback)
        {
            return Execute(manager, new CountdownTimer(), callback);
        }

        public static ConnectionHeartbeatRequest Execute(IConnectionManager manager, ICountdownTimer timer,
            Action<DateTimeOffset?, ErrorInfo> callback)
        {
            var request = new ConnectionHeartbeatRequest();

            if (manager.Connection.State != ConnectionStateType.Connected)
            {
                callback?.Invoke(default(DateTimeOffset), DefaultError);

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
                request._timer.Start(TimeSpan.FromMilliseconds(HeartbeatTimeout), request.OnTimeout);
            }

            return request;
        }

        private void OnMessageReceived(ProtocolMessage message)
        {
            if (CanHandleMessage(message))
            {
                FinishRequest(DateTimeOffset.MaxValue, null); //TODO: Fix when I see how the data is returned
            }
        }

        private void OnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (e.CurrentState != ConnectionStateType.Connected)
            {
                FinishRequest(default(DateTimeOffset), DefaultError);
            }
        }

        private void OnTimeout()
        {
            FinishRequest(null, DefaultError);
        }

        private void FinishRequest(DateTimeOffset? result, ErrorInfo error)
        {
            if (_finished == false)
            {
                lock (_syncLock)
                {
                    if (_finished == false)
                    {
                        _manager.MessageReceived -= OnMessageReceived;
                        _manager.Connection.ConnectionStateChanged -= OnConnectionStateChanged;
                        _timer.Abort();

                        _callback?.Invoke(result, error);

                        _callback = null;
                        _manager = null;
                        _timer = null;
                    }
                }
            }
            
        }
    }
}