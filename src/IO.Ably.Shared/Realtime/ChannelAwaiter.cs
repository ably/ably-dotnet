﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IO.Ably.Infrastructure;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;

namespace IO.Ably.Realtime
{
    internal class ChannelAwaiter : IDisposable
    {
        internal ILogger Logger { get; private set; }

        private readonly RealtimeChannel _channel;
        private readonly ChannelState _awaitedState;
        private readonly List<Action<bool, ErrorInfo>> _callbacks = new List<Action<bool, ErrorInfo>>();

        public bool Waiting { get; private set; }

        private readonly CountdownTimer _timer;
        private readonly string _name;
        private readonly object _lock = new object();
        private readonly Action _onTimeout;

        public ChannelAwaiter(IRealtimeChannel channel, ChannelState awaitedState, ILogger logger = null, Action onTimeout = null)
        {
            Logger = logger ?? DefaultLogger.LoggerInstance;
            _name = $"#{channel.Name}:{awaitedState} awaiter";
            _channel = channel as RealtimeChannel;
            _awaitedState = awaitedState;
            _timer = new CountdownTimer(_name + " timer", logger);
            _onTimeout = onTimeout;
            AttachListener();
        }

        public void Fail(ErrorInfo error)
        {
            Complete(false, error);
        }

        private void Complete(bool success, ErrorInfo error = null)
        {
            lock (_lock)
            {
                _timer?.Abort();
                if (Waiting == false)
                {
                    return;
                }

                Waiting = false;
            }

            if (error != null)
            {
                InvokeCallbacks(success, error);
            }
        }

        private void InvokeCallbacks(bool success, ErrorInfo error)
        {
            List<Action<bool, ErrorInfo>> callbacks;
            lock (_lock)
            {
                callbacks = _callbacks.ToList();
                _callbacks.Clear();
            }

            foreach (var callback in callbacks)
            {
                try
                {
                    callback?.Invoke(success, error);
                }
                catch (Exception e)
                {
                    Logger.Error("Error invoking callback for - " + _name, e);
                }
            }
        }

        public async Task<Result<bool>> WaitAsync(TimeSpan? timeout = null)
        {
            Func<Action<bool, ErrorInfo>, bool> func = action => StartWait(action, timeout ?? TimeSpan.FromSeconds(2));

            return await TaskWrapper.Wrap(func);
        }

        public bool StartWait(Action<bool, ErrorInfo> callback, TimeSpan timeout, bool restart = false)
        {
            if (_channel.State == _awaitedState && !restart)
            {
                try
                {
                    callback?.Invoke(true, null);
                }
                catch (Exception e)
                {
                    Logger.Error("Error invoking callback for - " + _name, e);
                }

                return false;
            }

            lock (_lock)
            {
                if (Waiting)
                {
                    Logger.Warning(
                        $"Awaiter for {_awaitedState} has been called multiple times. Adding action to callbacks");
                    _callbacks.Add(callback);
                    return false;
                }

                _timer.Start(timeout, ElapsedSync);
                Waiting = true;
                _callbacks.Add(callback);

                return true;
            }
        }

        private void ElapsedSync()
        {
            lock (_lock)
            {
                Waiting = false;
            }

            _onTimeout?.Invoke();

            InvokeCallbacks(false, new ErrorInfo("Timeout exceeded for " + _name, 50000));
        }

        private void AttachListener()
        {
            _channel.InternalStateChanged += ChannelOnChannelStateChanged;
        }

        private void DetachListener()
        {
            _channel.InternalStateChanged -= ChannelOnChannelStateChanged;
        }

        private void ChannelOnChannelStateChanged(object sender, ChannelStateChange args)
        {
            lock (_lock)
            {
                if (Waiting == false)
                {
                    return;
                }
            }

            if (args.Current == _awaitedState)
            {
                Complete(true);
                InvokeCallbacks(true, null);
            }
        }

        public void Dispose()
        {
            DetachListener();
            _timer?.Dispose();
        }
    }
}
