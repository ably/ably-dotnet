using Android.App;
using Android.Content;
using Firebase.Messaging;
using System;
using System.Diagnostics;

namespace DotnetPush.Droid
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class MyFirebaseMessaging : FirebaseMessagingService
    {

        private const String NotificationChannelId = "1152";
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
            // Expected JSON
            //	{
            //		"data": {
            //			"title": "Postman Test",
            //			"body": "Using Postman",
            //			"extraInfo": "Lalalala"
            //		}
            //	}

            Debugger.Break();
        }

        public const string PUSH_NOTIFICATION_ACTION = "MyFirebaseMessaging.PUSH_NOTIFICATION_MESSAGE";

        /// <summary>
        /// Handles the notification to ensure the Notification manager is updated to alert the user
        /// </summary>
        // private void SendNotification(PushNotification push) 
        // {
        // 	// Create relevant non-repeatable Id to allow multiple notifications to be displayed in the Notification Manager
        //
        // 	Intent intent = new Intent(this, typeof(MainActivity));
        // 	intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
        // 	intent.PutExtra("Title", push.Title);
        // 	intent.PutExtra("ExtraInfo", push.ExtraInfo);
        //
        // 	PendingIntent pendingIntent = PendingIntent.GetActivity(this, notificationId, intent, PendingIntentFlags.UpdateCurrent);
        // 	notificationManager = (NotificationManager)GetSystemService(Context.NotificationService);
        //
        // 	// Set BigTextStyle for expandable notifications
        // 	NotificationCompat.BigTextStyle bigTextStyle = new NotificationCompat.BigTextStyle();
        // 	bigTextStyle.SetSummaryText(push.Body);
        // 	bigTextStyle.SetSummaryText(String.Empty);
        //
        // 	Int64 timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        //
        // 	Notification notification = new NotificationCompat.Builder(this, NotificationChannelId)
        // 	.SetSmallIcon(Resource.Drawable.ic_launcher)
        // 	.SetContentTitle(push.Title)
        // 	.SetContentText(push.Body)
        // 	.SetStyle(bigTextStyle)
        // 	.SetPriority(NotificationCompat.PriorityHigh)
        // 	.SetWhen(timestamp)
        // 	.SetShowWhen(true)
        // 	.SetContentIntent(pendingIntent)
        // 	.SetAutoCancel(true)
        // 	.Build();
        //
        // 	notificationManager.Notify(notificationId, notification);
        // }
    }
}