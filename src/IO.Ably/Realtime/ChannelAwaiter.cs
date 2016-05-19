using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using IO.Ably.Transport;

namespace IO.Ably.Realtime
{
    internal class ChannelAwaiter
    {
        private readonly IRealtimeChannel _channel;
        private readonly ChannelState _awaitedState;
        private Stopwatch _stopwatch = new Stopwatch();
        private Action<TimeSpan, ErrorInfo> _callback;
        private volatile bool _waiting = false;

        public ChannelAwaiter(IRealtimeChannel channel, ChannelState awaitedState)
        {
            _channel = channel;
            _awaitedState = awaitedState;
        }

        public void Fail(ErrorInfo error)
        {
            if (_waiting && _stopwatch.IsRunning)
            {
                _stopwatch.Stop();
                _callback?.Invoke(_stopwatch.Elapsed, error);
            }
        }

        public void Wait(Action<TimeSpan, ErrorInfo> callback)
        {
            if(_waiting) 
                throw new AblyException("This awaiter is already in waiting state.");
            _waiting = true;
            _callback = callback;
            if (_channel.State == _awaitedState)
            {
                _waiting = false;
                callback?.Invoke(TimeSpan.Zero, null);
            }
            _stopwatch.Reset();
            _stopwatch.Start();
            AttachListener();
        }
        
        private void AttachListener()
        {
            _channel.ChannelStateChanged += ChannelOnChannelStateChanged;
        }

        private void DetachListener()
        {
            _channel.ChannelStateChanged -= ChannelOnChannelStateChanged;
        }

        private void ChannelOnChannelStateChanged(object sender, ChannelStateChangedEventArgs args)
        {
            if (args.NewState == _awaitedState)
            {
                _stopwatch.Stop();
                DetachListener();
                try
                {
                    _waiting = false;
                    _callback?.Invoke(_stopwatch.Elapsed, null);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error in callback for Channel state: " + _awaitedState, ex);
                }
            }
        }
    }
}
