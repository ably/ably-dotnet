using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotnetPush.Models;
using Foundation;
using IO.Ably;
using IO.Ably.Push;
using IO.Ably.Push.iOS;
using UIKit;
using UserNotifications;
using Xamarin.Essentials;
using Xamarin.Forms.Platform.iOS;

namespace DotnetPush.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : FormsApplicationDelegate,
        IUNUserNotificationCenterDelegate
    {
        private IRealtimeClient _realtime;
        private AppLoggerSink _loggerSink;
        private static readonly PushNotificationReceiver Receiver = new PushNotificationReceiver();

        private Task LogCallback(string name, ErrorInfo error)
        {
            var noError = error is null;
            _loggerSink.LogEvent(LogLevel.Debug, noError ? $"{name} callback." : $"{name} callback with error - {error.Message}");
            return Task.CompletedTask;
        }

        private void InitialiseAbly()
        {
            _loggerSink = new AppLoggerSink();

            var savedClientId = Preferences.Get("ABLY_CLIENT_ID", string.Empty, "Ably_Device");

            var callbacks = new PushCallbacks
            {
                ActivatedCallback = error => LogCallback("Activated", error),
                DeactivatedCallback = error => LogCallback("Deactivated", error),
                SyncRegistrationFailedCallback = error => LogCallback("SyncRegistrationFailed", error),
            };
            _realtime = AppleMobileDevice.Initialise(GetAblyOptions(savedClientId), callbacks);

            _realtime.Connect();
        }

        private ClientOptions GetAblyOptions(string savedClientId)
        {
            var options = new ClientOptions
            {
                // https://ably.com/documentation/best-practice-guide#auth
                // recommended for security reasons. Please, review Ably's best practise guide on Authentication
                // Please provide a way for AblyRealtime to connect to the services. Having API keys on mobile devices is not
                Key = "<key>",
                LogHandler = (ILoggerSink)_loggerSink,
                LogLevel = LogLevel.Debug,
                ClientId = string.IsNullOrWhiteSpace(savedClientId) ? Guid.NewGuid().ToString("D") : savedClientId,
            };

            _realtime = new AblyRealtime(options);
            _realtime.Connect();

            return options;
        }

        /// <inheritdoc/>
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            Xamarin.Forms.Forms.Init();
            InitialiseAbly();
            LoadApplication(new App(_realtime, _loggerSink, Receiver));

            UNUserNotificationCenter.Current.Delegate = this;

            return base.FinishedLaunching(app, options);
        }

        /// <inheritdoc/>
        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
            var token = deviceToken;
            AppleMobileDevice.OnNewRegistrationToken(token);
        }

        /// <inheritdoc/>
        public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        {
            var ablyError =
                new ErrorInfo($"Failed to get Registration token for push notifications: {error.LocalizedDescription}");

            AppleMobileDevice.OnRegistrationTokenFailed(ablyError);
        }

        // For versions previous to IOS10. It is otherwise deprecated.
        public override void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo)
        {
            NSDictionary aps = userInfo.ObjectForKey(new NSString("aps")) as NSDictionary;
            string alert = "Background: ";
            if (aps.ContainsKey(new NSString("alert")))
            {
                alert += ((NSString)aps[new NSString("alert")]).ToString();
            }

            if (!string.IsNullOrEmpty(alert))
            {
                UIAlertView avAlert = new UIAlertView("Notification", alert, null, "OK", null);
                avAlert.Show();
            }
        }

        /// <inheritdoc/>
        public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        {
            var notification = new PushNotification() { Received = DateTimeOffset.UtcNow };
            var alert = string.Empty;
            NSDictionary aps = userInfo.ObjectForKey(new NSString("aps")) as NSDictionary;
            if (IsDataNotification(aps))
            {
                alert += "Background";
                notification.Title = "Background notification";
                notification.Data = Convert(userInfo);
            }
            else
            {
                NSDictionary alertDetails = aps?.ObjectForKey(new NSString("alert")) as NSDictionary;
                notification.Title = GetProperty(alertDetails, "title");
                notification.Body = GetProperty(alertDetails, "body");
                notification.Data = Convert(aps);
                alert += "Foreground - " + notification.Title;
            }

            Receiver.Notify(notification);

            if (!string.IsNullOrEmpty(alert))
            {
                UIAlertView avAlert = new UIAlertView("Notification", alert, null, "OK", null);
                avAlert.Show();
            }

            bool IsDataNotification(NSDictionary dict) => dict.ContainsKey(new NSString("content-available"));

            string GetProperty(NSDictionary dict, string key)
            {
                if (dict is null)
                {
                    return string.Empty;
                }

                return dict.ContainsKey(new NSString(key))
                    ? ((NSString)dict[new NSString(key)]).ToString()
                    : string.Empty;
            }
        }

        private static Dictionary<string, string> Convert(NSDictionary nativeDict)
        {
            if (nativeDict is null)
            {
                return new Dictionary<string, string>();
            }

            return nativeDict.ToDictionary<KeyValuePair<NSObject, NSObject>, string, string>(
                item => (NSString)item.Key, item => item.Value.ToString());
        }
    }
}
