using System;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Ably.Transport.States.Connection
{
    public interface ICountdownTimer
    {
        void Start(int countdown, Action onTimeOut);
        void Abort();
    }

    internal delegate void TimerCallback(object state);

    //TODO: Replace with a .net framework provided timer.
    internal sealed class Timer : CancellationTokenSource, IDisposable
    {
        internal Timer(TimerCallback callback, object state, int dueTime, int period)
        {
            // Contract.Assert( period == -1, "This stub implementation only supports dueTime." );
            Task.Delay(dueTime, Token).ContinueWith((t, s) =>
            {
                var tuple = (Tuple<TimerCallback, object>) s;
                tuple.Item1(tuple.Item2);
            }, Tuple.Create(callback, state), CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Default);
        }

        public new void Dispose()
        {
            Cancel();
        }
    }

    public class CountdownTimer : ICountdownTimer
    {
        private Timer _timer;

        public void Start(int countdown, Action onTimeOut)
        {
            if (_timer != null)
            {
                Abort();
            }

            _timer = new Timer(o =>
            {
                onTimeOut();
                _timer.Dispose();
            }, null, countdown, Timeout.Infinite);
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