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
        /// <param name="realtimeClient">Ably client.</param>
        /// <param name="loggerSink">Instance of the AppLoggerSink so we can display and analyze logs inside the app.</param>
        public App(IRealtimeClient realtimeClient, AppLoggerSink loggerSink)
        {
            InitializeComponent();

            DependencyService.RegisterSingleton(realtimeClient);
            DependencyService.RegisterSingleton(loggerSink);
            MainPage = new AppShell();
        }
    }
}
