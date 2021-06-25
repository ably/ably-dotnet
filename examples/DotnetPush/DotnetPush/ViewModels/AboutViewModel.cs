using System;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace DotnetPush.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        public AboutViewModel()
        {
            Title = "About";
            var pushRealtime = Ably.GetPushRealtime();

            ActivatePush = new Command(() => pushRealtime.Activate());
            DeactivatePush = new Command(() => pushRealtime.Deactivate());
        }

        public ICommand ActivatePush { get; }
        public ICommand DeactivatePush { get; }
    }
}