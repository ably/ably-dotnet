using System;
using System.Threading.Tasks;

namespace IO.Ably
{
    internal static class TaskUtils
    {
        // http://stackoverflow.com/a/21648387/126995
        public static Task IgnoreExceptions(this Task task)
        {
            task.ContinueWith(
                c =>
                    {
                        var ignored = c.Exception;
                    },
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            return task;
        }

        // http://stackoverflow.com/a/30857843/29555
        public static Task<object> Convert<T>(this Task<T> task)
        {
            TaskCompletionSource<object> res = new TaskCompletionSource<object>();

            return task.ContinueWith(
                t =>
                    {
                        if (t.IsCanceled)
                        {
                            res.TrySetCanceled();
                        }
                        else if (t.IsFaulted)
                        {
                            res.TrySetException(t.Exception);
                        }
                        else
                        {
                            res.TrySetResult(t.Result);
                        }

                        return res.Task;
                    },
                TaskContinuationOptions.ExecuteSynchronously).Unwrap();
        }

        private static readonly Action<Task> DefaultErrorContinuation =
            t =>
            {
                try { t.Wait(); }
                catch { }
            };

        public static void RunInBackground(Action action, Action<Exception> handler = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var task = Task.Run(action);  // Adapt as necessary for .NET 4.0.

            if (handler == null)
            {
                task.ContinueWith(
                    DefaultErrorContinuation,
                    TaskContinuationOptions.ExecuteSynchronously |
                    TaskContinuationOptions.OnlyOnFaulted);
            }
            else
            {
                task.ContinueWith(
                    t => handler(t.Exception.GetBaseException()),
                    TaskContinuationOptions.ExecuteSynchronously |
                    TaskContinuationOptions.OnlyOnFaulted);
            }
        }
    }
}
