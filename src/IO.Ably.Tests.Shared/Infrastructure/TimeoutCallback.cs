using System;
using System.Threading.Tasks;

using IO.Ably.Tests.Infrastructure;

namespace IO.Ably.Tests
{
    internal class TimeoutCallback<T>
    {
        private readonly TaskCompletionAwaiter _tca;

        public TimeoutCallback(int timeout)
        {
            _tca = new TaskCompletionAwaiter(timeout);
        }

        public Task<bool> Task => _tca.Task;

        public Action<T> Wrap(Action<T> callback)
        {
            return csc =>
            {
                callback(csc);
                _tca.SetCompleted();
            };
        }
    }
}
