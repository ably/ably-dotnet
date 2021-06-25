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
            ActivatePush = new Command(() => Ably.Push.Activate());
            DeactivatePush = new Command(() => Ably.Push.Deactivate());
        }

        public ICommand ActivatePush { get; }
        public ICommand DeactivatePush { get; }
    }
}