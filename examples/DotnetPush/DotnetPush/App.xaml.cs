using IO.Ably;
using Xamarin.Forms;

namespace DotnetPush
{
    /// <summary>
    /// Xamarin Application entry point.
    /// </summary>
    public partial class App
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        /// <param name="realtimeClient">Instance of the Ably Realtime client.</param>
        /// <param name="appLoggerSink">Instance of the AppLoggerSink so we can display and analyze logs inside the app.</param>
        public App(AblyRealtime realtimeClient, AppLoggerSink appLoggerSink)
        {
            InitializeComponent();

            DependencyService.RegisterSingleton(realtimeClient);
            DependencyService.RegisterSingleton(appLoggerSink);
            MainPage = new AppShell();
        }
    }
}
