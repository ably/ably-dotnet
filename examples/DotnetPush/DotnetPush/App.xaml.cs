using System.Collections.Generic;
using DotnetPush.Services;
using IO.Ably;
using DotnetPush.Models;
using Xamarin.Forms;

namespace DotnetPush
{
    public class AppLoggerSink : ILoggerSink
    {
        public List<LogEntry> Messages { get; set; } = new List<LogEntry>();

        public void LogEvent(LogLevel level, string message)
        {
            Messages.Add(new LogEntry(level, message));
        }
    }

    public partial class App : Application
    {
        private readonly AblyRealtime _realtimeClient;

        public App(AblyRealtime realtimeClient, AppLoggerSink appLoggerSink)
        {
            _realtimeClient = realtimeClient;
            InitializeComponent();


            DependencyService.Register<MockDataStore>();
            DependencyService.RegisterSingleton(_realtimeClient);
            DependencyService.RegisterSingleton(appLoggerSink);
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
