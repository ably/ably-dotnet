using System;
using System.Threading.Tasks;

namespace IO.Ably
{
    internal class AblyAuthUpdatedEventArgs : EventArgs
    {
        public TokenDetails Token { get; set; }

        /// <summary>
        /// Gets the TaskCompletionSource for this event
        /// A handler should Complete this task to allow the
        /// AuthorizeAsync call to complete
        /// </summary>
        internal TaskCompletionSource<bool> CompletedTask { get; }

        public AblyAuthUpdatedEventArgs()
        {
            CompletedTask = new TaskCompletionSource<bool>();
        }

        public AblyAuthUpdatedEventArgs(TokenDetails token)
            : this()
        {
            Token = token;
        }

        public void CompleteAuthorization(bool success)
        {
            CompletedTask.TrySetResult(success);
        }
    }
}