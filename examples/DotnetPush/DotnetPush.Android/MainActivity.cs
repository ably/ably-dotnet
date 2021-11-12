using System;
using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using IO.Ably.Push.Android;
using Firebase;
using IO.Ably;
using IO.Ably.Push;
using Xamarin.Essentials;

namespace DotnetPush.Droid
{
    [Activity(Label = "DotnetPush", Icon = "@mipmap/logo", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        internal static PushNotificationReceiver Receiver = new PushNotificationReceiver();
        private AppLoggerSink _loggerSink;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _loggerSink = new AppLoggerSink();
            Platform.Init(this, savedInstanceState);
            Xamarin.Forms.Forms.Init(this, savedInstanceState);

            // Initialise the Firebase application
            FirebaseApp.InitializeApp(this);
            var realtime = Configure(new PushCallbacks());
            LoadApplication(new App(realtime, _loggerSink, Receiver));
        }

        public IRealtimeClient Configure(PushCallbacks callbacks)
        {
            var options = new ClientOptions();
            options.LogHandler = _loggerSink;
            options.LogLevel = LogLevel.Debug;
            // Using an API on a mobile device is not best practise.
            // Better provider an AuthUrl so a backend deals with storing keys
            options.Key = "GJvITg.ZwK8rQ:VqRp350wVM13-sQb";

            // If we have already created a clientId for this instance then load it back.
            var savedClientId = AblySettings.ClientId;
            if (string.IsNullOrWhiteSpace(savedClientId) == false)
            {
                options.ClientId = savedClientId;
            }
            else
            {
                options.ClientId = Guid.NewGuid().ToString("D");
                AblySettings.ClientId = options.ClientId; // Save it for later use.
            }

            return AndroidMobileDevice.Initialise(options, callbacks);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}