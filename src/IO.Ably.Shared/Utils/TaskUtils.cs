using System.Threading.Tasks;

namespace IO.Ably
{
    internal static class TaskUtils
    {
        // http://stackoverflow.com/a/21648387/126995
        public static Task IgnoreExceptions( this Task task )
        {
            task.ContinueWith( c => { var ignored = c.Exception; },
                TaskContinuationOptions.OnlyOnFaulted |
                TaskContinuationOptions.ExecuteSynchronously );
            return task;
        }

        // http://stackoverflow.com/a/30857843/29555
        public static Task<object> Convert<T>(this Task<T> task)
        {
            TaskCompletionSource<object> res = new TaskCompletionSource<object>();

            return task.ContinueWith(t =>
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
            }

            , TaskContinuationOptions.ExecuteSynchronously).Unwrap();
        }
    }
}