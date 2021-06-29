using System.Windows.Input;
using Xamarin.Forms;

namespace DotnetPush.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        private string _clientId;
        private string _currentState;

        public AboutViewModel()
        {
            Title = "About";
            var pushRealtime = Ably.GetPushRealtime();
            ClientId = Ably.Auth.ClientId;
            Ably.GetPushRealtime().OnActivationStateMachineChangeState((current, next) =>
            {
                CurrentState = next;
            });

            ActivatePush = new Command(() => pushRealtime.Activate());
            DeactivatePush = new Command(() => pushRealtime.Deactivate());
        }

        public ICommand ActivatePush { get; }
        public ICommand DeactivatePush { get; }

        public string ClientId
        {
            get => _clientId;
            set => SetProperty(ref _clientId, value);
        }

        public string CurrentState
        {
            get => _currentState;
            set => SetProperty(ref _currentState, value);
        }
    }
}