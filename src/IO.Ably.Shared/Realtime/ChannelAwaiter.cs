using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using IO.Ably.Transport;

namespace IO.Ably.Realtime
{
    internal class ChannelAwaiter
    {
        private readonly RealtimeChannel _channel;
        private readonly ChannelState _awaitedState;
        private readonly ConcurrentBag<Action<bool, ErrorInfo>> _callbacks = new ConcurrentBag<Action<bool, ErrorInfo>>();
        private volatile bool _waiting;

        public ChannelAwaiter(IRealtimeChannel channel, ChannelState awaitedState)
        {
            _channel = channel as RealtimeChannel;
            _awaitedState = awaitedState;
        }

        public void Fail(ErrorInfo error)
        {
            if (_waiting)
            {
                InvokeCallbacks(false, error);
            }
        }

        private void InvokeCallbacks(bool success, ErrorInfo error)
        {
            foreach (var callback in _callbacks)
            {
                callback?.Invoke(success, error);
            }
        }

        public async Task<Result<bool>> WaitAsync(TimeSpan? timeout = null)
        {
            var wrappedTask = TaskWrapper.Wrap<bool, bool>(StartWait);

            var first = await Task.WhenAny(Task.Delay(timeout ?? TimeSpan.FromSeconds(2)), wrappedTask);
            if (first == wrappedTask)
                return wrappedTask.Result;

            return Result.Fail<bool>(new ErrorInfo("Timeout exceeded", 50000));
        }

        public bool StartWait(Action<bool, ErrorInfo> callback)
        {
            if (_waiting)
            {
                Logger.Warning($"Awaiter for {_awaitedState} has been called multiple times. Most likely a concurrency issue.");
                _callbacks.Add(callback);
                return false;
            }

            _waiting = true;
            _callbacks.Add(callback);
            if (_channel.State == _awaitedState)
            {
                _waiting = false;
                InvokeCallbacks(true, null);
                return false;
            }
            AttachListener();
            return true;
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
            if (args.Current == _awaitedState)
            {
                DetachListener();
                try
                {
                    _waiting = false;
                    InvokeCallbacks(true, null);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error in callback for Channel state: " + _awaitedState, ex);
                }
            }
        }
    }
}
