using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nito.AsyncEx.Internal.PlatformEnlightenment
{
    public sealed class SingleThreadedApartmentThread
    {
        private readonly object _thread;

        public SingleThreadedApartmentThread(Action execute, bool sta)
        {
            _thread = sta ? new ThreadTask(execute) : (object)Task.Factory.StartNew(execute, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public Task JoinAsync()
        {
            var ret = _thread as Task;
            if (ret != null)
                return ret;
            return ((ThreadTask)_thread).Task;
        }

        private sealed class ThreadTask
        {
            private readonly TaskCompletionSource<object> _tcs;
            private readonly Thread _thread;

            public ThreadTask(Action execute)
            {
                _tcs = new TaskCompletionSource<object>();
                _thread = new Thread(() =>
                {
                    try
                    {
                        execute();
                    }
                    finally
                    {
                        _tcs.TrySetResult(null);
                    }
                });
#if !HASTHREADING
                _thread.IsBackground = true;
#endif
#if HASTHREADING
                _thread.SetApartmentState(ApartmentState.STA);
#endif
                _thread.Name = "STA AsyncContextThread (Nito.AsyncEx)";
                _thread.Start();
            }

            public Task Task
            {
                get { return _tcs.Task; }
            }
        }
    }
}
