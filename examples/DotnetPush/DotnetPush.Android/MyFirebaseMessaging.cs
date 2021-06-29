using Android.App;
using Android.Content;
using Android.Support.V4.App;
using Firebase.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DotnetPush.Droid
{
    public class PushNotification
    {
        public string Title { get; set; }
        public string Body { get; set; }

        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();

    }

    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class MyFirebaseMessaging : FirebaseMessagingService
    {
        private string notificationChannelId = "AblyChannel"; // Random number - don't know if it needs to be specific
        private NotificationManager notificationManager;


        public override void OnNewToken(String token)
        {
            Debugger.Break();
        }

        /// <summary>
        /// When the app receives a notification, this method is called
        ///
        /// </summary>
        public override void OnMessageReceived(RemoteMessage remoteMessage)
        {
            var notification = remoteMessage.GetNotification();
            var title = notification.Title;
            var body = notification.Body;

            PushNotification push = new PushNotification
            {
                Title = title ?? "",
                Body = body ?? "",

            };

            if (remoteMessage.Data.Count >= 1)
            {
                push.Data = new Dictionary<string, string> (remoteMessage.Data);

                SendNotification(push);
            }
        }

        public const string PUSH_NOTIFICATION_ACTION = "MyFirebaseMessaging.PUSH_NOTIFICATION_MESSAGE";

        /// <summary>
        /// Handles the notification to ensure the Notification manager is updated to alert the user
        /// </summary>
        private void SendNotification(PushNotification push)
        {
            // Create relevant non-repeatable Id to allow multiple notifications to be displayed in the Notification Manager

            Intent intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
            intent.PutExtra("Title", push.Title);
            intent.PutExtra("Body", push.Body);

            var notificationId = int.Parse(DateTime.Now.ToString("MMddHHmmsss"));

            PendingIntent pendingIntent = PendingIntent.GetActivity(this, notificationId, intent, PendingIntentFlags.UpdateCurrent);
            notificationManager = (NotificationManager)GetSystemService(Context.NotificationService);

            // Set BigTextStyle for expandable notifications
            NotificationCompat.BigTextStyle bigTextStyle = new NotificationCompat.BigTextStyle();
            bigTextStyle.SetSummaryText(push.Body);
            bigTextStyle.SetSummaryText(String.Empty);

            Int64 timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            Notification notification = new NotificationCompat.Builder(this, notificationChannelId)
            .SetSmallIcon(Resource.Drawable.ably_logo)
            .SetContentTitle(push.Title)
            .SetContentText(push.Body)
            .SetStyle(bigTextStyle)
            .SetPriority(NotificationCompat.PriorityHigh)
            .SetWhen(timestamp)
            .SetShowWhen(true)
            .SetContentIntent(pendingIntent)
            .SetAutoCancel(true)
            .Build();

            notificationManager.Notify(notificationId, notification);
        }
    }
}