using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using IO.Ably;
using Xamarin.Forms;

namespace DotnetPush.ViewModels
{
    /// <summary>
    /// Base view model class. It implements INotificationPropertyChanged and exposes SetProperty method which
    /// makes implementing notification properties easier.
    /// </summary>
    public class BaseViewModel : INotifyPropertyChanged
    {
        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Resolves an instance of AblyRealtime.
        /// </summary>
        public IRealtimeClient Ably => DependencyService.Get<IRealtimeClient>();

        private bool _isBusy = false;

        /// <summary>
        /// Whether the page is busy i.e. there is a background operation which we are waiting to complete.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string _title = string.Empty;

        /// <summary>
        /// Page title.
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// Set Property value and trigger PropertyChanged event if needed.
        /// </summary>
        /// <param name="backingStore">Property backing store.</param>
        /// <param name="value">New property value.</param>
        /// <param name="propertyName">Property name.</param>
        /// <param name="onChanged">Action to be executed if the property has changed.</param>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <returns>`True` if the property has changed and `False` if it hasn't.</returns>
        protected bool SetProperty<T>(
            ref T backingStore,
            T value,
            [CallerMemberName] string propertyName = "",
            Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
            {
                return false;
            }

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Triggers PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">name of the property which value has changed.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed is null)
            {
                return;
            }

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
