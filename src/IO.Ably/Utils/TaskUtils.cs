using System.Threading.Tasks;

namespace IO.Ably
{
    static class TaskUtils
    {
        // http://stackoverflow.com/a/21648387/126995
        public static Task IgnoreExceptions( this Task task )
        {
            task.ContinueWith( c => { var ignored = c.Exception; },
                TaskContinuationOptions.OnlyOnFaulted |
                TaskContinuationOptions.ExecuteSynchronously );
            return task;
        }
    }
}