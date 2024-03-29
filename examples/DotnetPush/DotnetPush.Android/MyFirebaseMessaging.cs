﻿using Android.App;
using Android.Content;
using Android.Support.V4.App;
using Firebase.Messaging;
using System;
using System.Collections.Generic;
using DotnetPush.Models;
using IO.Ably.Push.Android;

namespace DotnetPush.Droid
{
    /// <summary>
    /// Implementation of FirebaseMessagingService which is used to receive messages from Firebase Cloud Messaging.
    /// For more information visit https://firebase.google.com/docs/reference/android/com/google/firebase/messaging/FirebaseMessagingService.
    /// </summary>
    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class MyFirebaseMessaging : FirebaseMessagingService
    {
        private const string NotificationChannelId = "AblyChannel";
        private NotificationManager _notificationManager;

        /// <summary>
        /// Called when a new token for the default Firebase project is generated.
        /// </summary>
        /// <param name="token">The new token.</param>
        public override void OnNewToken(string token)
        {
            AndroidMobileDevice.OnNewRegistrationToken(token);
        }

        /// <summary>
        /// Called every time a remote message is received.
        /// </summary>
        /// <param name="remoteMessage">A remote firebase message. (https://firebase.google.com/docs/reference/android/com/google/firebase/messaging/RemoteMessage).</param>
        public override void OnMessageReceived(RemoteMessage remoteMessage)
        {
            var notification = remoteMessage.GetNotification();

            var title = notification?.Title;
            var body = notification?.Body;

            PushNotification push = new PushNotification
            {
                Title = title ?? string.Empty,
                Body = body ?? string.Empty,
                Data = new Dictionary<string, string>(remoteMessage.Data),
                Received = DateTimeOffset.Now
            };

            MainActivity.Receiver.Notify(push);

            if (remoteMessage.Data.Count >= 1)
            {
                SendNotification(push);
            }
        }

        /// <summary>
        /// Handles the notification to ensure the Notification manager is updated to alert the user.
        /// </summary>
        /// <param name="push">Helper method to display the received push notification on the device.</param>
        private void SendNotification(PushNotification push)
        {
            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
            intent.PutExtra("Title", push.Title);
            intent.PutExtra("Body", push.Body);

            var notificationId = int.Parse(DateTime.Now.ToString("MMddHHmmsss"));

            PendingIntent pendingIntent =
                PendingIntent.GetActivity(this, notificationId, intent, PendingIntentFlags.UpdateCurrent);
            _notificationManager = (NotificationManager)GetSystemService(Context.NotificationService);

            // Set BigTextStyle for expandable notifications
            NotificationCompat.BigTextStyle bigTextStyle = new NotificationCompat.BigTextStyle();
            bigTextStyle.SetSummaryText(push.Body);
            bigTextStyle.SetSummaryText(string.Empty);

            long timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            Notification notification = new NotificationCompat.Builder(this, NotificationChannelId)
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

            _notificationManager?.Notify(notificationId, notification);
        }
    }
}
