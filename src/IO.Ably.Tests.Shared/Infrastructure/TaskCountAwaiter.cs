using System;
using System.Threading.Tasks;

namespace IO.Ably.Tests.Infrastructure
{
    /// <summary>
    /// Count a certain number of ticks and complete or timeout
    /// </summary>
    internal class TaskCountAwaiter
    {
        private TaskCompletionAwaiter _awaiter;
        public int Start { get; } = 0;
        public int Index { get; private set; } = 0;

        public TaskCountAwaiter(int count, int timeoutMs = 10000)
        {
            Start = count;
            if(Start < 1)
            {
                throw new Exception("count must be 1 or more");
            }

            Index = count;
            _awaiter = new TaskCompletionAwaiter(timeoutMs);
        }

        public void Tick()
        {
            Index--;
            if (Index == 0)
            {
                _awaiter.SetCompleted();
            }
        }

        public Task<bool> Task => _awaiter.Task;
    }
}
