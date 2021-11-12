using DotnetPush.Models;

namespace DotnetPush
{
    /// <summary>
    /// Wrapper class for ReceivedPushNotification event handler.
    /// </summary>
    public class PushNotificationReceiver
    {
        /// <summary>
        /// Push notification handler.
        /// </summary>
        /// <param name="notification">Notification.</param>
        public delegate void PushNotificationHandler(PushNotification notification);

        /// <summary>
        /// Handles PushNotificationEvents.
        /// </summary>
        public event PushNotificationHandler ReceivedPushNotification = _ => { };

        /// <summary>
        /// Trigger notification about a push message.
        /// </summary>
        /// <param name="notification">PushNotification that was received.</param>
        public void Notify(PushNotification notification) => ReceivedPushNotification?.Invoke(notification);
    }
}
