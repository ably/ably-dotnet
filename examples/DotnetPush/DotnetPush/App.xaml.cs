using DotnetPush.Services;
using DotnetPush.Views;
using IO.Ably;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DotnetPush
{
    public partial class App : Application
    {

        private readonly AblyRealtime _realtimeClient;

        public App(AblyRealtime realtimeClient)
        {
            _realtimeClient = realtimeClient;
            InitializeComponent();

            DependencyService.Register<MockDataStore>();
            DependencyService.RegisterSingleton(_realtimeClient);
            MainPage = new AppShell();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
