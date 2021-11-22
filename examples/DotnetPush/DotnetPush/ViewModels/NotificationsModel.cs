using System.Collections.ObjectModel;
using DotnetPush.Models;
using Xamarin.Forms;

namespace DotnetPush.ViewModels
{
    /// <summary>
    /// View model for the Log page.
    /// </summary>
    public class NotificationsModel : BaseViewModel
    {
        /// <summary>
        /// The current NotificationReceiver.
        /// </summary>
        public PushNotificationReceiver NotificationReceiver => DependencyService.Get<PushNotificationReceiver>();

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationsModel"/> class.
        /// </summary>
        public NotificationsModel()
        {
            Notifications = new ObservableCollection<PushNotification>();
            NotificationReceiver.ReceivedPushNotification += Notifications.Add;
        }

        /// <summary>
        /// Observable collection of LogEntries.
        /// </summary>
        public ObservableCollection<PushNotification> Notifications { get; set; }
    }
}
