using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Assets.Tests.AblySandbox
{
    public sealed class ConditionalAwaiter : IDisposable
    {
        private readonly Func<bool> _condition;
        private readonly Timer _timer;
        private readonly TaskCompletionSource<bool> _completionSource;
        private int _tickCount;

        public ConditionalAwaiter(Func<bool> condition)
        {
            _condition = condition;
            _timer = new Timer
            {
                Enabled = true,
                Interval = 100,
            };
            _timer.Elapsed += TimerOnElapsed;
            _completionSource = new TaskCompletionSource<bool>();
        }

        public TaskAwaiter<bool> GetAwaiter()
        {
            return _completionSource.Task.GetAwaiter();
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            Interlocked.Increment(ref _tickCount);
            if (_tickCount > 100)
            {
                _completionSource.SetException(new Exception("Timer elapsed. Giving up."));
            }

            if (_condition() && _completionSource.Task.IsCompleted == false)
            {
                _timer.Enabled = false;
                _completionSource.SetResult(true);
                _timer.Elapsed -= TimerOnElapsed;
                _timer.Dispose();
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
