using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using IO.Ably;
using IO.Ably.Push.Android;
using Firebase;
using Xamarin.Essentials;
using static Android.Provider.Settings;

namespace DotnetPush.Droid
{
    [Activity(Label = "DotnetPush", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        private AblyRealtime _realtime;
        private AppLoggerSink _loggerSink;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _loggerSink = new AppLoggerSink();
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            // Initialise the Firebase application
            FirebaseApp.InitializeApp(this);
            InitialiseAbly();
            LoadApplication(new App(_realtime, _loggerSink));
        }

        private void InitialiseAbly()
        {
            AndroidMobileDevice.Initialise();
            var savedClientId = Preferences.Get("ABLY_CLIENT_ID", "", "Ably_Device");
            var options = new ClientOptions();
            options.Key = "GJvITg.Jnks6w:IUoxrgaHjIw5LHQG";
            options.LogHandler = _loggerSink;
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

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}