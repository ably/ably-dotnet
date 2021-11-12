using System.Collections.ObjectModel;
using System.Threading.Tasks;
using IO.Ably;
using IO.Ably.Push;
using Xamarin.Forms;
using static DotnetPush.Infrastructure.Helpers;

namespace DotnetPush.ViewModels
{
    /// <summary>
    /// Describes a channel in the UI.
    /// </summary>
    public class AblyChannel
    {
        /// <summary>
        /// Name of the ably channel.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of the channel.</param>
        public AblyChannel(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// View model for the Log page.
    /// </summary>
    public class ChannelsViewModel : BaseViewModel
    {
        private string _channelName;
        private string _message;
        private bool _messageIsVisible;

        /// <summary>
        /// Command to Load log entries.
        /// </summary>
        public Command LoadChannelsCommand { get; }

        /// <summary>
        /// Subscribes current device to channel.
        /// </summary>
        public Command SubscribeToChannel { get; }

        /// <summary>
        /// Unsubscribe from a channel.
        /// </summary>
        public Command UnSubscribeFromChannel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogViewModel"/> class.
        /// </summary>
        public ChannelsViewModel()
        {
            ChannelsCollection = new ObservableCollection<AblyChannel>();
            UnSubscribeFromChannel = new Command<string>(async channelName =>
            {
                await Ably.Channels.Get(channelName).Push.UnsubscribeDevice();
                await ExecuteLoadItemsCommand(); // Make sure we reload the channels list

                Message = $"Unsubscribed from channel {channelName}";
                MessageIsVisible = true;

                DelayAction(() =>
                {
                    Message = string.Empty;
                    MessageIsVisible = false;
                    return Task.CompletedTask;
                });
            });
            LoadChannelsCommand = new Command(async () => await ExecuteLoadItemsCommand());
            SubscribeToChannel = new Command(async () =>
            {
                MessageIsVisible = false;

                if (string.IsNullOrEmpty(ChannelName))
                {
                    Message = "Please enter a channel name";
                    return;
                }

                if (ChannelName.StartsWith("push:") == false)
                {
                    Message = "Make sure the 'push:' channel namespace is set.";
                    return;
                }

                try
                {
                    await Ably.Channels.Get(ChannelName).Push.SubscribeDevice();
                    await ExecuteLoadItemsCommand(); // Make sure we reload the channels list
                }
                catch (AblyException e)
                {
                    Message = $"Error subscribing device to channel. Messages: {e.Message}. Code: {e.ErrorInfo.Code}";
                }

                ChannelName = string.Empty;
            });
        }

        /// <summary>
        /// Observable collection of LogEntries.
        /// </summary>
        public ObservableCollection<AblyChannel> ChannelsCollection { get; set; }

        /// <summary>
        /// Channel name.
        /// </summary>
        public string ChannelName
        {
            get => _channelName;
            set => SetProperty(ref _channelName, value);
        }

        /// <summary>
        /// Message.
        /// </summary>
        public string Message
        {
            get => _message;
            set
            {
                if (string.IsNullOrWhiteSpace(_message) == false)
                {
                    MessageIsVisible = true;
                }

                SetProperty(ref _message, value);
            }
        }

        /// <summary>
        /// Show / Hide message label.
        /// </summary>
        public bool MessageIsVisible
        {
            get => _messageIsVisible;
            set => SetProperty(ref _messageIsVisible, value);
        }

        private async Task ExecuteLoadItemsCommand()
        {
            IsBusy = true;

            try
            {
                ChannelsCollection.Clear();
                var device = Ably.Device;

                if (device.IsRegistered == false)
                {
                    Message = "Cannot get subscriptions when the local device is not registered";
                    return;
                }

                // For this information you need to have PushAdmin permissions for the app. Usually that will not be the case for mobile app. I'm adding it here because
                // it helps with debugging.
                var subscriptions = await Ably.Push.Admin.ChannelSubscriptions.ListAsync(ListSubscriptionsRequest.WithDeviceId(channel: null, device.Id));
                foreach (var subscription in subscriptions.Items)
                {
                    ChannelsCollection.Add(new AblyChannel(subscription.Channel));
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Executed before the View is displayed.
        /// </summary>
        public void OnAppearing()
        {
            IsBusy = true;
        }
    }
}
