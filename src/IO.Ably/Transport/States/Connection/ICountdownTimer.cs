using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Ably.Transport.States.Connection
{
    public interface ICountdownTimer
    {
        Task Start(int countdown, Action onTimeOut);
        void Abort();
    }

    internal sealed class Timer : CancellationTokenSource, IDisposable
    {
        private readonly Action _callback;
        private readonly TimeSpan _period;

        internal Timer(Action callback, TimeSpan period)
        {
            if(callback == null)
                throw new ArgumentNullException(nameof(callback));
            if (period == TimeSpan.Zero)
                throw new ArgumentException("Period can't be zero!", nameof(period));

            _callback = callback;
            _period = period;
        }

        public async Task Start()
        {
            while (!IsCancellationRequested)
            {
                await Task.Delay(_period, Token);

                if (!IsCancellationRequested)
                    _callback();
            }
        }

        public new void Dispose()
        {
            Cancel();
        }
    }

    public class CountdownTimer : ICountdownTimer
    {
        private Timer _timer;

        public Task Start(int countdownInMilliseconds, Action onTimeOut)
        {
            if (_timer != null)
            {
                Abort();
            }

            _timer = new Timer(onTimeOut, TimeSpan.FromMilliseconds(countdownInMilliseconds));
            return _timer.Start();
        }

        public void Abort()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }
    }
}