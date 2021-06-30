using System;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably.Infrastructure;

namespace IO.Ably.Transport.States.Connection
{
    /// <summary>
    /// Internal interface used for countdown timer.
    /// </summary>
    public interface ICountdownTimer
    {
        /// <summary>
        /// Starts a timer.
        /// </summary>
        /// <param name="delay">when to fire.</param>
        /// <param name="onTimeOut">action to execute.</param>
        void Start(TimeSpan delay, Action onTimeOut);

        /// <summary>
        /// Starts a timer with async onTimeOut method.
        /// </summary>
        /// <param name="delay">when to fire.</param>
        /// <param name="onTimeOut">async action to execute.</param>
        void StartAsync(TimeSpan delay, Func<Task> onTimeOut);

        /// <summary>
        /// Aborts the timer.
        /// </summary>
        /// <param name="trigger">whether to trigger the action. Default: false.</param>
        void Abort(bool trigger = false);
    }

    internal class CountdownTimer : ICountdownTimer, IDisposable
    {
        internal ILogger Logger { get; private set; }

        private readonly string _name;
        private Timer _timer;
        private Action _elapsedSync;
        private Func<Task> _elapsedAsync;
        private TimeSpan _delay;
        private bool _aborted;
        private readonly object _lock = new object();

        public CountdownTimer(string name, ILogger logger)
        {
            Logger = logger ?? DefaultLogger.LoggerInstance;
            _name = name;
        }

        public void Start(TimeSpan delay, Action elapsedSync)
        {
            if (elapsedSync == null)
            {
                throw new ArgumentNullException(nameof(elapsedSync));
            }

            if (Logger.IsDebug)
            {
                Logger.Debug($"Setting up timer '{_name}' to run action after {delay.TotalSeconds} seconds.");
            }

            _elapsedSync = elapsedSync;

            if (_timer != null)
            {
                Abort();
            }

            _timer = StartTimer(delay);
        }

        private Timer StartTimer(TimeSpan delay)
        {
            lock (_lock)
            {
                _aborted = false;
            }

            _delay = delay;
            var timer = new Timer(state => OnTimerOnElapsed(), null, (int)delay.TotalMilliseconds, Timeout.Infinite);
            return timer;
        }

        private void OnTimerOnElapsed()
        {
            // Ignore the result but don't allow the Countdown timer to take down the process if an exception is thrown
            // You never know what will happen if the Logger throws an exception :)
            _ = OnTimerOnElapsedAsync();
        }

        private async Task OnTimerOnElapsedAsync()
        {
            lock (_lock)
            {
                if (_aborted)
                {
                    if (Logger.IsDebug)
                    {
                        Logger.Debug($"Timer '{_name}' aborted. Skipping OnElapsed callback.");
                    }

                    return;
                }
            }

            try
            {
                if (_elapsedSync != null)
                {
                    if (Logger.IsDebug)
                    {
                        Logger.Debug($"Timer '{_name}' interval {_delay.TotalSeconds} seconds elapsed and calling action.");
                    }

                    _elapsedSync();
                }
                else
                {
                    if (Logger.IsDebug)
                    {
                        Logger.Debug($"Timer '{_name}' interval {_delay.TotalSeconds} seconds elapsed and calling async action.");
                    }

                    await _elapsedAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in method called by timer.", ex);
            }
        }

        public void StartAsync(TimeSpan delay, Func<Task> onTimeOut)
        {
            if (onTimeOut == null)
            {
                throw new ArgumentNullException(nameof(onTimeOut));
            }

            if (Logger.IsDebug)
            {
                Logger.Debug($"Setting up timer '{_name}' to run action after {delay.TotalSeconds} seconds");
            }

            if (_timer != null)
            {
                Abort();
            }

            _elapsedAsync = onTimeOut;

            _timer = StartTimer(delay);
        }

        public void Abort(bool trigger = false)
        {
            if (trigger)
            {
                OnTimerOnElapsed();
            }

            lock (_lock)
            {
                _aborted = true;
            }

            if (Logger.IsDebug)
            {
                Logger.Debug($"Aborting timer '{_name}'");
            }

            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
