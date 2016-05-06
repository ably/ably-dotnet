using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace IO.Ably.Transport.States.Connection
{
    public interface ICountdownTimer
    {
        void Start(TimeSpan delay, Action onTimeOut, bool autoReset = false);
        void StartAsync(TimeSpan delay, Func<Task> onTimeOut, bool autoReset = false);
        void Abort();
    }

    //internal sealed class Timer : CancellationTokenSource, IDisposable
    //{
    //    private readonly Func<Task> _callback;
    //    private readonly TimeSpan _period;
    //    private readonly Action _action;

    //    internal Timer(Func<Task> callback, TimeSpan period)
    //    {
    //        if(callback == null)
    //            throw new ArgumentNullException(nameof(callback));
    //        if (period == TimeSpan.Zero)
    //            throw new ArgumentException("Period can't be zero!", nameof(period));

    //        _callback = callback;
    //        _period = period;
    //    }

    //    internal Timer(Action action, TimeSpan period)
    //    {
    //        if (action == null)
    //            throw new ArgumentNullException(nameof(action));
    //        if (period == TimeSpan.Zero)
    //            throw new ArgumentException("Period can't be zero!", nameof(period));

    //        _action = action;
    //        _period = period;
    //    }

    //    public async Task Start()
    //    {
    //        while (!IsCancellationRequested)
    //        {
    //            await Task.Delay(_period, Token);

    //            if (!IsCancellationRequested)
    //            {
    //                if(_callback != null)
    //                    await _callback();

    //                _action?.Invoke();
    //            }
    //        }
    //    }

    //    public new void Dispose()
    //    {
    //        Cancel();
    //    }
    //}

    public class CountdownTimer : ICountdownTimer
    {
        private readonly string _name;
        private System.Timers.Timer _timer;
        private Action _onTimeOut;
        private Func<Task> _onTimeOutFunc;
        private TimeSpan _delay;

        public CountdownTimer(string name)
        {
            _name = name;
        }

        public void Start(TimeSpan delay, Action onTimeOut, bool autoReset = false)
        {
            if (onTimeOut == null)
                throw new ArgumentNullException(nameof(onTimeOut));

            if (Logger.IsDebug)
            {
                Logger.Debug($"Setting up timer '{_name}' to run action after {delay.TotalSeconds} seconds. Autoreset: {autoReset}");
            }

            _onTimeOut = onTimeOut;

            if (_timer != null)
            {
                Abort();
            }

            _timer = SetupTimer(delay, autoReset);
            _timer.Start();
        }

        private System.Timers.Timer SetupTimer(TimeSpan delay, bool autoReset)
        {
            _delay = delay;
            var timer = new System.Timers.Timer(delay.TotalMilliseconds);
            timer.AutoReset = autoReset;
            timer.Elapsed += OnTimerOnElapsed;
            return timer;
        }

        private async void OnTimerOnElapsed(object sender, ElapsedEventArgs args)
        {
            try
            {
                if (_onTimeOut != null)
                {
                    if (Logger.IsDebug)
                    {
                        Logger.Debug($"Timer '{_name}' interval {_delay.TotalSeconds} seconds elapsed and calling action.");    
                    }

                    _onTimeOut();
                }
                else
                {
                    if (Logger.IsDebug)
                    {
                        Logger.Debug($"Timer '{_name}' interval {_delay.TotalSeconds} seconds elapsed and calling async action.");
                    }
                    await _onTimeOutFunc();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in method called by timer.", ex);
                throw;
            }
        }
        

        public void StartAsync(TimeSpan delay, Func<Task> onTimeOut, bool autoReset = false)
        {
            if (onTimeOut == null)
                throw new ArgumentNullException(nameof(onTimeOut));

            if (Logger.IsDebug)
            {
                Logger.Debug($"Setting up timer '{_name}' to run action after {delay.TotalSeconds} seconds. Autoreset: {autoReset}");
            }

            if (_timer != null)
            {
                Abort();
            }

            _onTimeOutFunc = onTimeOut;

            _timer = SetupTimer(delay, autoReset);
            _timer.Start();
        }

        public void Abort()
        {
            if (Logger.IsDebug)
            {
                Logger.Debug($"Aborting timer '{_name}'");
            }
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
        }
    }
}