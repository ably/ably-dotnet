using System;
using System.Threading;

namespace Ably.Transport.States.Connection
{
    public interface ICountdownTimer
    {
        void Start(int countdown, Action onTimeOut);
        void Abort();
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
