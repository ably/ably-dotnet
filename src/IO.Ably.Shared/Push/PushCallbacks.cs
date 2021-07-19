using System;
using System.Threading.Tasks;

namespace IO.Ably.Push
{
    /// <summary>
    /// Class used to setup Push state change callbacks.
    /// </summary>
    public class PushCallbacks
    {
        /// <summary>
        /// Action called when the device has been deactivated for push notifications.
        /// Error info is either `null` or holds the current error.
        /// </summary>
        public Func<ErrorInfo, Task> DeactivatedCallback { get; set; } = async error => { };

        /// <summary>
        /// Action called when the device has been activated for push notifications.
        /// Error info is either `null` or holds the current error.
        /// </summary>
        public Func<ErrorInfo, Task> ActivatedCallback { get; set; } = async error => { };
    }
}
