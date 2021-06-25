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
                        else if (t.IsFaulted && t.Exception != null)
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

        /// <summary>
        /// Run a <see cref="Task"/> in a fire-and-forget manner.
        /// An optional handler can be provided to process exceptions, if they occur.
        /// </summary>
        internal static void RunInBackground(Task task, Action<Exception> handler = null)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

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
