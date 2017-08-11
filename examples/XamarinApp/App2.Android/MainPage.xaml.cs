using System;
using Xamarin.Forms;

namespace App2
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel viewModel;
        public MainPage(IAblyService ably)
        {
            InitializeComponent();
            var connectionStateObserver = new ConnectionStatusObserver(x => viewModel.ConnectionStatus = x);
            ably.Subscribe(connectionStateObserver);

            BindingContext = viewModel = new MainViewModel(ably);
        }
    }

    public class ConnectionStatusObserver : IObserver<string>
    {
        private readonly Action<string> _onChange;

        public ConnectionStatusObserver(Action<string> onChange)
        {
            _onChange = onChange;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(string value)
        {
            _onChange.Invoke(value);
        }
    }
}
