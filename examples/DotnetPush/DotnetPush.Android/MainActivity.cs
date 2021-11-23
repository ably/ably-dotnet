using System;
using System.Threading.Tasks;
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
    /// <summary>
    /// Xamarin forms MainActivity.
    /// </summary>
    [Activity(Label = "DotnetPush", Icon = "@mipmap/logo", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        internal static PushNotificationReceiver Receiver = new PushNotificationReceiver();
        private AppLoggerSink _loggerSink;

        private Task LogCallback(string name, ErrorInfo error)
        {
            var noError = error is null;
            _loggerSink.LogEvent(LogLevel.Debug, noError ? $"{name} callback." : $"{name} callback with error - {error.Message}");
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _loggerSink = new AppLoggerSink();
            Platform.Init(this, savedInstanceState);
            Xamarin.Forms.Forms.Init(this, savedInstanceState);

            // Initialise the Firebase application
            FirebaseApp.InitializeApp(this);
            var callbacks = new PushCallbacks()
            {
                ActivatedCallback = error => LogCallback("Activated", error),
                DeactivatedCallback = error => LogCallback("Deactivated", error),
                SyncRegistrationFailedCallback = error => LogCallback("SyncRegistrationFailed", error)
            };
            var realtime = Configure(callbacks);
            LoadApplication(new App(realtime, _loggerSink, Receiver));
        }

        /// <summary>
        /// Configures a Realtime client and initialises <see cref="AndroidMobileDevice"/> which facilitates push notifications.
        /// </summary>
        /// <param name="callbacks">Callbacks for when the device is Activated, Deactivated or a SyncRegistration failed.</param>
        /// <returns>returns a realtime client which can be used in the app.</returns>
        public IRealtimeClient Configure(PushCallbacks callbacks)
        {
            var options = new ClientOptions
            {
                LogHandler = _loggerSink,
                LogLevel = LogLevel.Debug,

                // https://ably.com/documentation/best-practice-guide#auth
                // recommended for security reasons. Please, review Ably's best practise guide on Authentication
                // Please provide a way for AblyRealtime to connect to the services. Having API keys on mobile devices is not
                Key = "<key>"
            };

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

        /// <inheritdoc/>
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
