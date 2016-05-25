using IO.Ably;
using IO.Ably.Realtime;
using System;
using System.Threading;
using System.Windows;
using IO.Ably.Rest;

namespace RealtimeChat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SynchronizationContext _appContext;

        public MainWindow()
        {
            _appContext = SynchronizationContext.Current;
            InitializeComponent();

            payloadBox.Text = "{\"handle\":\"Your Name\",\"message\":\"Testing Message\"}";
        }

        private AblyRealtime client;
        private IRealtimeChannel channel;

        private async void Subscribe_Click(object sender, RoutedEventArgs e)
        {
            string channelName = this.channelBox.Text.Trim();
            if (string.IsNullOrEmpty(channelName))
            {
                return;
            }

            string key = RealtimeChat.Properties.Settings.Default.ApiKey;
            string clientId = RealtimeChat.Properties.Settings.Default.ClientId;
            var options = new ClientOptions(key) { UseBinaryProtocol = false, Tls = true, AutoConnect = false, ClientId = clientId };
            this.client = new AblyRealtime(options);
            this.client.Connection.ConnectionStateChanged += this.connection_ConnectionStateChanged;
            this.client.Connect();

            this.channel = this.client.Channels.Get(channelName);
            this.channel.StateChanged += channel_ChannelStateChanged;
            this.channel.Subscribe(Handler);
            this.channel.Presence.MessageReceived += this.Presence_MessageReceived;
            await channel.AttachAsync();
            await channel.Presence.Enter("test data");
        }

        private void Handler(Message message)
        {
            outputBox.Items.Add($"Message: {message}");
        }

        private void Trigger_Click(object sender, RoutedEventArgs e)
        {
            string eventName = this.eventBox.Text.Trim();
            string payload = this.payloadBox.Text.Trim();

            if (this.channel == null || string.IsNullOrEmpty(eventName))
            {
                return;
            }

            channel.Publish(eventName, payload);
        }

        private void connection_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            outputBox.Items.Add(string.Format("Connection: {0}", e.CurrentState));
        }

        private void channel_ChannelStateChanged(object sender, ChannelStateChangedEventArgs e)
        {
            outputBox.Items.Add(string.Format("Channel: {0}", e.NewState));
        }

        void Presence_MessageReceived(PresenceMessage[] messages)
        {
            _appContext.Post(delegate
            {
                foreach (PresenceMessage message in messages)
                {
                    outputBox.Items.Add(string.Format("{0}: {1} {2}", message.data, message.action, message.clientId));
                }
            }, null);
        }
    }
}
