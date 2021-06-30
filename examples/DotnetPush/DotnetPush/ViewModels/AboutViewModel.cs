using System.Windows.Input;
using Xamarin.Forms;

namespace DotnetPush.ViewModels
{
    /// <summary>
    /// ViewModel class for the About page.
    /// </summary>
    public class AboutViewModel : BaseViewModel
    {
        private string _clientId;
        private string _currentState;

        /// <summary>
        /// Initializes a new instance of the <see cref="AboutViewModel"/> class.
        /// </summary>
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

        /// <summary>
        /// Command which will call AblyRealtime.Push.Activate().
        /// </summary>
        public ICommand ActivatePush { get; }

        /// <summary>
        /// Command which will call AblyRealtime.Push.Deactivate().
        /// </summary>
        public ICommand DeactivatePush { get; }

        /// <summary>
        /// Displays current clientId that is set in the library.
        /// </summary>
        public string ClientId
        {
            get => _clientId;
            set => SetProperty(ref _clientId, value);
        }

        /// <summary>
        /// Displays the current State of the ActivationStateMachine. It's only updated
        /// and doesn't show the current loaded state.
        /// </summary>
        public string CurrentState
        {
            get => _currentState;
            set => SetProperty(ref _currentState, value);
        }
    }
}
