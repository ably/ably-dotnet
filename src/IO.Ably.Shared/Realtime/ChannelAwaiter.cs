using System;
using System.Threading.Tasks;
using IO.Ably.Transport;

namespace IO.Ably.Realtime
{
    internal class ChannelAwaiter
    {
        private readonly RealtimeChannel _channel;
        private readonly ChannelState _awaitedState;
        private Action<bool, ErrorInfo> _callback;
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
                _callback?.Invoke(false, error);
            }
        }

        public async Task<Result<bool>> WaitAsync(TimeSpan? timeout = null)
        {
            var wrappedTask = TaskWrapper.Wrap<bool, bool>(Wait);
                
                var first = await Task.WhenAny(Task.Delay(timeout ?? TimeSpan.FromSeconds(2)), wrappedTask);
                if (first == wrappedTask)
                    return wrappedTask.Result;

                return Result.Fail<bool>(new ErrorInfo("Timeout exceeded", 50000));
        }

        public bool Wait(Action<bool, ErrorInfo> callback)
        {
            if (_waiting)
            {
                Logger.Warning($"Awaiter for {_awaitedState} has been called multiple times. Most likely a consurrency issue.");
                return false;
            }

            _waiting = true;
            _callback = callback;
            if (_channel.State == _awaitedState)
            {
                _waiting = false;
                callback?.Invoke(true, null);
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
                    _callback?.Invoke(true, null);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error in callback for Channel state: " + _awaitedState, ex);
                }
            }
        }
    }
}
