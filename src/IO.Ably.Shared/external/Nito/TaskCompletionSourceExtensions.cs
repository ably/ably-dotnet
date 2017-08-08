using System;
using System.Threading.Tasks;
using Nito.AsyncEx.Internal;

namespace Nito.AsyncEx
{
    /// <summary>
    /// Provides extension methods for <see cref="TaskCompletionSource{TResult}"/>.
    /// </summary>
    public static class TaskCompletionSourceExtensions
    {
        /// <summary>
        /// Attempts to complete a <see cref="TaskCompletionSource{TResult}"/>, propagating the completion of <paramref name="task"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the target asynchronous operation.</typeparam>
        /// <typeparam name="TSourceResult">The type of the result of the source asynchronous operation.</typeparam>
        /// <param name="this">The task completion source. May not be <c>null</c>.</param>
        /// <param name="task">The task. May not be <c>null</c>.</param>
        /// <returns><c>true</c> if this method completed the task completion source; <c>false</c> if it was already completed.</returns>
        public static bool TryCompleteFromCompletedTask<TResult, TSourceResult>(this TaskCompletionSource<TResult> @this, Task<TSourceResult> task) where TSourceResult : TResult
        {
            if (task.IsFaulted)
                return @this.TrySetException(task.Exception.InnerExceptions);
            if (task.IsCanceled)
                return @this.TrySetCanceled();
            return @this.TrySetResult(task.Result);
        }

        /// <summary>
        /// Attempts to complete a <see cref="TaskCompletionSource"/>, propagating the completion of <paramref name="task"/>.
        /// </summary>
        /// <param name="this">The task completion source. May not be <c>null</c>.</param>
        /// <param name="task">The task. May not be <c>null</c>.</param>
        /// <returns><c>true</c> if this method completed the task completion source; <c>false</c> if it was already completed.</returns>
        public static bool TryCompleteFromCompletedTask(this TaskCompletionSource @this, Task task)
        {
            if (task.IsFaulted)
                return @this.TrySetException(task.Exception.InnerExceptions);
            if (task.IsCanceled)
                return @this.TrySetCanceled();
            return @this.TrySetResult();
        }

        /// <summary>
        /// Attempts to complete a <see cref="TaskCompletionSource{TResult}"/> with the specified value, forcing all continuations onto a threadpool thread even if they specified <c>ExecuteSynchronously</c>.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the asynchronous operation.</typeparam>
        /// <param name="this">The task completion source. May not be <c>null</c>.</param>
        /// <param name="result">The result of the asynchronous operation.</param>
        public static void TrySetResultWithBackgroundContinuations<TResult>(this TaskCompletionSource<TResult> @this, TResult result)
        {
            // Set the result on a threadpool thread, so any synchronous continuations will execute in the background.
            TaskShim.Run(() => @this.TrySetResult(result));

            // Wait for the TCS task to complete; note that the continuations may not be complete.
            @this.Task.Wait();
        }

        /// <summary>
        /// Attempts to complete a <see cref="TaskCompletionSource"/>, forcing all continuations onto a threadpool thread even if they specified <c>ExecuteSynchronously</c>.
        /// </summary>
        /// <param name="this">The task completion source. May not be <c>null</c>.</param>
        public static void TrySetResultWithBackgroundContinuations(this TaskCompletionSource @this)
        {
            // Set the result on a threadpool thread, so any synchronous continuations will execute in the background.
            TaskShim.Run(() => @this.TrySetResult());

            // Wait for the TCS task to complete; note that the continuations may not be complete.
            @this.Task.Wait();
        }

        /// <summary>
        /// Attempts to complete a <see cref="TaskCompletionSource{TResult}"/> as canceled, forcing all continuations onto a threadpool thread even if they specified <c>ExecuteSynchronously</c>.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the asynchronous operation.</typeparam>
        /// <param name="this">The task completion source. May not be <c>null</c>.</param>
        public static void TrySetCanceledWithBackgroundContinuations<TResult>(this TaskCompletionSource<TResult> @this)
        {
            // Complete on a threadpool thread, so any synchronous continuations will execute in the background.
            TaskShim.Run(() => @this.TrySetCanceled());

            // Wait for the TCS task to complete; note that the continuations may not be complete.
            try
            {
                @this.Task.Wait();
            }
            catch (AggregateException)
            {
            }
        }
        
    }
}
