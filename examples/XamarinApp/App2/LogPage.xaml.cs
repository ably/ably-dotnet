using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace App2
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LogPage : ContentPage
    {
        private readonly IAblyService _ably;

        public LogPage(IAblyService ably)
        {
            _ably = ably;
            InitializeComponent();
            var logObserver = new LogObserver(message => LogList.Text = LogList.Text + $"[{message.Level}] " + message.Message);

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
            throw new NotImplementedException();
        }

        public void OnNext(LogMessage value)
        {
            _onMessage(value);
        }
    }
}