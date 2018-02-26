using System;
using System.Net;
using System.Threading;

using IO.Ably;
using IO.Ably.Realtime;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;

namespace IO.Ably.Transport
{
    internal class ConnectionHeartbeatRequest
    {
        public static readonly ErrorInfo DefaultError = new ErrorInfo("Unable to ping service; not connected", 40000,
            HttpStatusCode.BadRequest);
        public static readonly ErrorInfo TimeOutError = new ErrorInfo("Unable to ping service; Request timed out", 40800, HttpStatusCode.RequestTimeout);

        private Action<TimeSpan?, ErrorInfo> _callback;
        private ConnectionManager _manager;
        private ICountdownTimer _timer;
        private bool _finished;
        private object _syncLock = new object();
        private DateTimeOffset _start = DateTimeOffset.MinValue;
        internal Func<DateTimeOffset> Now { get; set; }
        internal ILogger Logger { get; private set; }

        public ConnectionHeartbeatRequest(ConnectionManager manager, ICountdownTimer timer, Func<DateTimeOffset> nowFunc)
        {
            Logger = manager.Logger;
            _manager = manager;
            _timer = timer;
            Now = nowFunc;
        }

        public static bool CanHandleMessage(ProtocolMessage message)
        {
            return message.Action == ProtocolMessage.MessageAction.Heartbeat;
        }

        public static ConnectionHeartbeatRequest Execute(ConnectionManager manager, Func<DateTimeOffset> nowProvider, Action<TimeSpan?, ErrorInfo> callback)
        {
            return Execute(manager, new CountdownTimer("Connection heartbeat timer", manager.Logger), nowProvider, callback);
        }

        public static ConnectionHeartbeatRequest Execute(ConnectionManager manager, ICountdownTimer timer,
            Func<DateTimeOffset> nowProvider, Action<TimeSpan?, ErrorInfo> callback)
        {
            var request = new ConnectionHeartbeatRequest(manager, timer, nowProvider);
            return request.Send(callback);
        }

        private ConnectionHeartbeatRequest Send(Action<TimeSpan?, ErrorInfo> callback)
        {
            _start = Now();

            if (_manager.Connection.State != ConnectionState.Connected)
            {
                callback?.Invoke(default(TimeSpan), DefaultError);

                return this;
            }

            if (callback != null)
            {
                _callback = callback;
                _manager.MessageReceived += OnMessageReceived;
                _manager.Connection.InternalStateChanged += OnInternalStateChanged;

                _manager.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat), null);
                _timer.Start(_manager.DefaultTimeout, () => FinishRequest(null, TimeOutError));
            }

            return this;
        }

        private void OnMessageReceived(ProtocolMessage message)
        {
            if (CanHandleMessage(message))
            {
                FinishRequest(GetElapsedTime(), null);
            }
        }

        private TimeSpan? GetElapsedTime()
        {
            return Now() - _start;
        }

        private void OnInternalStateChanged(object sender, ConnectionStateChange e)
        {
            if (e.Current != ConnectionState.Connected)
            {
                FinishRequest(default(TimeSpan), DefaultError);
            }
        }

        private void FinishRequest(TimeSpan? result, ErrorInfo error)
        {
            if (_finished == false)
            {
                lock (_syncLock)
                {
                    if (_finished == false)
                    {
                        _finished = true;

                        _manager.MessageReceived -= OnMessageReceived;
                        _manager.Connection.InternalStateChanged -= OnInternalStateChanged;
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