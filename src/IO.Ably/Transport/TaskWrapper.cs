using System;
using System.Threading.Tasks;

namespace IO.Ably.Transport
{
    /// <summary>This trivial class wraps legacy callback-style API into a Task API.</summary>
    internal class TaskWrapper
    {
        readonly TaskCompletionSource<Result> _completionSource = new TaskCompletionSource<Result>();

        public Task<Result> Task => _completionSource.Task;

        public void Callback(bool res, ErrorInfo ei)
        {
            if (res)
                _completionSource.SetResult(Result.Ok());
            else if (ei != null)
                _completionSource.SetResult(Result.Fail(ei));
            else
                _completionSource.SetException(new Exception("Unexpected exception thrown by the TaskWrapper."));
        }

        public static Task<Result<T>> Wrap<TParam, T>(TParam obj, Action<TParam, Action<T, ErrorInfo>> toWrapMethod)
        {
            var wrapper = new TaskWrapper<T>();
            try
            {
                toWrapMethod(obj, wrapper.Callback);
            }
            catch (Exception ex)
            {
                wrapper.SetException(ex);
            }

            return wrapper.Task;
        }


    public static Task<Result<T>> Wrap<T>(Action<Action<T, ErrorInfo>> toWrapMethod)
        {
            var wrapper = new TaskWrapper<T>();
            try
            {
                toWrapMethod(wrapper.Callback);
            }
            catch (Exception ex)
            {
                wrapper.SetException(ex);
            }
            
            return wrapper.Task;
        }
    }

    internal class TaskWrapper<T>
    {
        readonly TaskCompletionSource<Result<T>> _completionSource = new TaskCompletionSource<Result<T>>();

        public Task<Result<T>> Task => _completionSource.Task;

        public void Callback(T res, ErrorInfo ei)
        {
            if (ei != null)
                _completionSource.SetResult(Result.Fail<T>(ei));
            else if (typeof(T).IsValueType && Equals(res, default(T)) == false)
                _completionSource.SetResult(Result.Ok(res));
            else if (typeof(T).IsValueType == false && res != null)
                _completionSource.SetResult(Result.Ok(res));
            else
                _completionSource.SetException(new Exception("")); //Something bad happened
        }


        public void SetException(Exception ex)
        {
            _completionSource.SetException(new AblyException(ex));
        }
    }
}