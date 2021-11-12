using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using IO.Ably;
using IO.Ably.Push.iOS;
using UIKit;
using Xamarin.Essentials;

namespace DotnetPush.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        private AblyRealtime _realtime;
        private AppLoggerSink _loggerSink;

        private void InitialiseAbly()
        {
            _loggerSink = new AppLoggerSink();

            AblyAppleMobileDevice.Initialise();
            var savedClientId = Preferences.Get("ABLY_CLIENT_ID", "", "Ably_Device");
            var options = new ClientOptions();
            options.Key = "GJvITg.Jnks6w:IUoxrgaHjIw5LHQG"; // TODO: Remove and delete Martin's app.
            options.LogHandler = (ILoggerSink)_loggerSink;
            options.LogLevel = LogLevel.Debug;
            // This is just to make testing easier.
            // In a normal app this will usually be set to Secure.GetString(ContentResolver, Secure.AndroidId);
            if (string.IsNullOrWhiteSpace(savedClientId) == false)
            {
                options.ClientId = savedClientId;
            }
            else
            {
                options.ClientId = Guid.NewGuid().ToString("D");
            }
            _realtime = new AblyRealtime(options);
            _realtime.Connect();
        }

        //
        // This method is invoked when the application has loaded and is ready to run. In this
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            global::Xamarin.Forms.Forms.Init();
            InitialiseAbly();
            LoadApplication(new App(_realtime, _loggerSink));

            return base.FinishedLaunching(app, options);
        }

        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
            var token = deviceToken;
            AblyAppleMobileDevice.OnNewRegistrationToken(token, _realtime);
        }

        public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        {
            var ablyError =
                new ErrorInfo($"Failed to get Registration token for push notifications: {error.LocalizedDescription}");

            AblyAppleMobileDevice.OnRegistrationTokenFailed(ablyError, _realtime);
        }

        public override void ReceivedRemoteNotification (UIApplication application, NSDictionary userInfo)
        {
            NSDictionary aps = userInfo.ObjectForKey(new NSString("aps")) as NSDictionary;
            string alert = "Background: ";
            if (aps.ContainsKey(new NSString("alert")))
                alert += (aps[new NSString("alert")] as NSString).ToString();
            //show alert
            if (!string.IsNullOrEmpty(alert))
            {
                UIAlertView avAlert = new UIAlertView("Notification", alert, null, "OK", null);
                avAlert.Show();
            }
        }

        public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        {
            NSDictionary aps = userInfo.ObjectForKey(new NSString("aps")) as NSDictionary;
            string alert = "Foreground: ";
            if (aps.ContainsKey(new NSString("title")))
                alert += (aps[new NSString("alert")] as NSString).ToString();
            //show alert
            if (!string.IsNullOrEmpty(alert))
            {
                UIAlertView avAlert = new UIAlertView("Notification", alert, null, "OK", null);
                avAlert.Show();
            }
        }

        public void ShowErrorMessage(string message)
        {
            var alertWindow = new UIWindow(UIScreen.MainScreen.Bounds);
            alertWindow.RootViewController = new UIViewController();

            var alertController = UIAlertController.Create("Error", message, UIAlertControllerStyle.Alert);
            alertController.AddAction(UIAlertAction.Create("Close", UIAlertActionStyle.Cancel, _ => alertWindow.Hidden = true));

            alertWindow.WindowLevel = UIWindowLevel.Alert + 1;
            alertWindow.MakeKeyAndVisible();
            alertWindow.RootViewController?.PresentViewController(alertController, true, null);
        }
    }
}
