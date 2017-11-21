using System;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Ably.Tests.Infrastructure
{
    public class TaskCompletionAwaiter : IDisposable
    {
        private CancellationTokenSource _cancellationTokenSource;
        private TaskCompletionSource<bool> _taskCompletionSource;

        public int TimeoutMs { get; private set; }

        public TaskCompletionAwaiter(int timeoutMs = 10000)
        {
            TimeoutMs = timeoutMs;

            _cancellationTokenSource = new CancellationTokenSource(TimeoutMs);
            _taskCompletionSource = new TaskCompletionSource<bool>();
            _cancellationTokenSource.Token.Register(() => _taskCompletionSource.TrySetResult(false));
        }
        
        public void SetCompleted()
        {
            _taskCompletionSource.TrySetResult(true);
        }

        public void SetFailed()
        {
            _taskCompletionSource.TrySetResult(false);
        }

        public Task<bool> Task => _taskCompletionSource.Task;

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
            _taskCompletionSource = null;
        }
    }
}