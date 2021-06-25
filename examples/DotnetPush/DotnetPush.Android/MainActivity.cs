using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using IO.Ably;
using IO.Ably.Push.Android;
using Firebase;
using static Android.Provider.Settings;

namespace DotnetPush.Droid
{
    [Activity(Label = "DotnetPush", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        private AblyRealtime _realtime;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            InitialiseAbly();
            LoadApplication(new App(_realtime));
        }

        private void InitialiseAbly()
        {
            
            FirebaseApp.InitializeApp(this);
            AndroidMobileDevice.Initialise();
            var options = new ClientOptions();
            options.Key = "GJvITg.Jnks6w:IUoxrgaHjIw5LHQG";
            options.ClientId = Secure.GetString(ContentResolver, Secure.AndroidId);
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