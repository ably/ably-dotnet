using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace App2.Droid
{
    [Activity(Label = "App2", Icon = "@drawable/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        static void HandleExceptions(object sender, UnhandledExceptionEventArgs e)
        {
            //Exception d = (Exception)e.ExceptionObject;
            Android.Util.Log.Debug("ERROR", "App error: " + e.ExceptionObject.ToString());
        }

        public MainActivity()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += HandleExceptions;
        }

        public AblyService ablyService = null;
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);
            ablyService = new AblyService();
            ablyService.Init();
            LoadApplication(new App(ablyService));
        }
    }
}

