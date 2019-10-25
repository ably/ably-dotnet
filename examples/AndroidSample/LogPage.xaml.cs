using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace AndroidSample
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LogPage : ContentPage
    {
        private readonly AblyService _ably;

        public LogPage(AblyService ably)
        {
            _ably = ably;
            InitializeComponent();
            var logObserver = new LogObserver(message => LogList.Text = LogList.Text + $"[{message.Level}] " + message.Message + "\n");

            _ably.Subscribe(logObserver);
        }
    }

    public class LogObserver : IObserver<LogMessage>
    {
        private readonly Action<LogMessage> _onMessage;

        public LogObserver(Action<LogMessage> onMessage)
        {
            _onMessage = onMessage;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {

        }

        public void OnNext(LogMessage value)
        {
            _onMessage(value);
        }
    }
}