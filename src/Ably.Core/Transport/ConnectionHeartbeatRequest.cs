using Ably.Transport.States.Connection;
using Ably.Types;
using System;

namespace Ably.Transport
{
    internal class ConnectionHeartbeatRequest
    {
        private IConnectionManager manager;
        private Action<bool, ErrorInfo> callback;
        private ICountdownTimer timer;

        private static readonly ErrorInfo DefaultError = new ErrorInfo("Unable to ping service; not connected", 40000, System.Net.HttpStatusCode.BadRequest);
        private const int HeartbeatTimeout = 5 * 1000;

        public static bool CanHandleMessage(ProtocolMessage message)
        {
            return message.Action == ProtocolMessage.MessageAction.Heartbeat;
        }

        public static ConnectionHeartbeatRequest Execute(IConnectionManager manager, Action<bool, ErrorInfo> callback)
        {
            return Execute(manager, new CountdownTimer(), callback);
        }

        public static ConnectionHeartbeatRequest Execute(IConnectionManager manager, ICountdownTimer timer, Action<bool, ErrorInfo> callback)
        {
            ConnectionHeartbeatRequest request = new ConnectionHeartbeatRequest();

            if (manager.Connection.State != Realtime.ConnectionState.Connected)
            {
                if (callback != null)
                {
                    callback(false, DefaultError);
                }
                return request;
            }

            if (callback != null)
            {
                request.manager = manager;
                request.manager.MessageReceived += request.OnMessageReceived;
                request.manager.Connection.ConnectionStateChanged += request.OnConnectionStateChanged;
                request.timer = timer;
                request.callback = callback;
            }
            manager.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat), null);
            if (callback != null)
            {
                request.timer.Start(HeartbeatTimeout, request.OnTimeout);
            }

            return request;
        }

        private void OnMessageReceived(ProtocolMessage message)
        {
            if (CanHandleMessage(message))
            {
                this.FinishRequest(true, null);
            }
        }

        private void OnConnectionStateChanged(object sender, Realtime.ConnectionStateChangedEventArgs e)
        {
            if (e.CurrentState != Realtime.ConnectionState.Connected)
            {
                this.FinishRequest(false, DefaultError);
            }
        }

        private void OnTimeout()
        {
            this.FinishRequest(false, DefaultError);
        }

        private void FinishRequest(bool result, ErrorInfo error)
        {
            this.manager.MessageReceived -= this.OnMessageReceived;
            this.manager.Connection.ConnectionStateChanged -= this.OnConnectionStateChanged;
            this.timer.Abort();

            if (this.callback != null)
            {
                this.callback(result, error);
            }

            this.callback = null;
            this.manager = null;
            this.timer = null;
        }
    }
}
