using System;
using System.Threading.Tasks;
using DotnetPush.Models;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace DotnetPush.ViewModels
{
    public class StateViewModel : BaseViewModel
    {
        private readonly Action<string> _displayAlert;
        private StateModel _state;
        public Command LoadStateCommand { get; }
        public Command ClearStateCommand { get; set; }

        public StateViewModel(Action<string> displayAlert)
        {
            _displayAlert = displayAlert;
            LoadStateCommand = new Command(async () => await LoadState());
            ClearStateCommand = new Command(async () => await ClearState());
        }

        private async Task ClearState()
        {
            Preferences.Clear("Ably_StateMachine");
            Preferences.Clear("Ably_Device");
            _displayAlert("State cleared. Restart the application");
            await LoadState();
        }

        public StateModel State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }

        private async Task LoadState()
        {
            string GetStateMachineProperty(string key) => Preferences.Get(key, "[not set]", "Ably_StateMachine");
            string GetDeviceProperty(string key) => Preferences.Get(key, "[not set]", "Ably_Device");


            var model = new StateModel();
            model.StateMachine.CurrentState = GetStateMachineProperty("ABLY_PUSH_CURRENT_STATE");
            model.StateMachine.PendingEvents = GetStateMachineProperty("ABLY_PUSH_PENDING_EVENTS");

            model.Device.Token = GetDeviceProperty("ABLY_REGISTRATION_TOKEN");
            model.Device.TokenType = GetDeviceProperty("ABLY_REGISTRATION_TOKEN_TYPE");
            model.Device.DeviceToken = GetDeviceProperty("ABLY_DEVICE_IDENTITY_TOKEN");
            model.Device.DeviceSecret = GetDeviceProperty("ABLY_DEVICE_SECRET");
            model.Device.ClientId = GetDeviceProperty("ABLY_CLIENT_ID");
            model.Device.DeviceId = GetDeviceProperty("ABLY_DEVICE_ID");

            State = model;
        }

        public void OnAppearing()
        {
            _ = LoadState();
        }
    }
}
