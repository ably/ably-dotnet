using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Assets.Tests.AblySandbox
{
    public sealed class TaskCompletionAwaiter : IDisposable
    {
        private CancellationTokenSource _cancellationTokenSource;
        private TaskCompletionSource<bool> _taskCompletionSource;

        public int TimeoutMs { get; private set; }

        public int TaskCount { get; private set; }

        /// <summary>
        /// Wait for task(s) to complete
        /// </summary>
        /// <param name="timeoutMs">set the timeout in MS</param>
        /// <param name="taskCount">sets the number of tasks. Used with Tick().
        /// Tick() will need to be called this number of times before a timeout to be considered a success.</param>
        public TaskCompletionAwaiter(int timeoutMs = 10000, int taskCount = 1)
        {
            TimeoutMs = timeoutMs;

            if (taskCount < 1)
            {
                throw new ArgumentException("taskCount must be greater than zero");
            }

            TaskCount = taskCount;

            _cancellationTokenSource = new CancellationTokenSource(TimeoutMs);
            _taskCompletionSource = new TaskCompletionSource<bool>();
            _cancellationTokenSource.Token.Register(() => _taskCompletionSource.TrySetResult(false));
        }

        public void Done()
        {
            SetCompleted();
        }

        public void Tick()
        {
            TaskCount--;
            if (TaskCount < 1)
            {
                SetCompleted();
            }
        }

        public void Set(bool value)
        {
            _taskCompletionSource.TrySetResult(value);
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

        public TaskAwaiter<bool> GetAwaiter()
        {
            return _taskCompletionSource.Task.GetAwaiter();
        }

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
            _taskCompletionSource = null;
        }
    }
}