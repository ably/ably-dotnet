using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace IO.Ably.Tests.Infrastructure
{
    public class ConditionalAwaiter
    {
        private readonly Func<bool> _condition;
        private readonly Func<string> _getError;
        private Timer _timer = new Timer();
        private TaskCompletionSource<bool> _completionSource;
        private int _tickCount = 0;

        public ConditionalAwaiter(Func<bool> condition, Func<string> getError = null)
        {
            _condition = condition;
            _getError = getError;
            _timer.Enabled = true;
            _timer.Interval = 100;
            _timer.Elapsed += TimerOnElapsed;
            _completionSource = new TaskCompletionSource<bool>();
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            Interlocked.Increment(ref _tickCount);
            if (_tickCount > 100)
            {
                _completionSource.SetException(new Exception("10 seconds elapsed. Giving up. Error: " + _getError?.Invoke()));
            }

            if (_condition() && _completionSource.Task.IsCompleted == false)
            {
                _timer.Enabled = false;
                _completionSource.SetResult(true);
                _timer.Elapsed -= TimerOnElapsed;
                _timer.Dispose();
            }
        }

        public TaskAwaiter<bool> GetAwaiter()
        {
            return _completionSource.Task.GetAwaiter();
        }
    }
}
