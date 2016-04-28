using System;
using System.Threading.Tasks;
using IO.Ably.Transport.States.Connection;

namespace IO.Ably.Tests
{
    internal class FakeTimer : ICountdownTimer
    {
        public Action OnTimeOut { get; private set; }
        public Func<Task> OnTimeOutFunc { get; private set; }
        public bool AutoRest { get; set; }

        public TimeSpan LastDelay { get; set; }

        public bool StartedWithAction { get; set; }

        public bool StartedWithFunc { get; set; }

        public bool Aborted { get; set; }

        public void Start(TimeSpan delay, Action onTimeOut, bool autoReset = false)
        {
            OnTimeOut = onTimeOut;
            StartedWithAction = true;
            LastDelay = delay;
            AutoRest = autoReset;
        }

        public void StartAsync(TimeSpan delay, Func<Task> onTimeOut, bool autoReset = false)
        {
            OnTimeOutFunc = onTimeOut;
            StartedWithFunc = true;
            LastDelay = delay;
            AutoRest = autoReset;
        }

        public void Abort()
        {
            Aborted = true;
        }
    }
}