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
        private readonly Func<string> _getError;
        private readonly Timer _timer;
        private readonly TaskCompletionSource<bool> _completionSource;
        private int _tickCount;

        public ConditionalAwaiter(Func<bool> condition, Func<string> getError = null)
        {
            _condition = condition;
            _getError = getError;
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
                string message = "10 seconds elapsed. Giving up.";
                if (_getError != null)
                {
                    message += " Error: " + _getError();
                }

                _completionSource.SetException(new Exception(message));
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
