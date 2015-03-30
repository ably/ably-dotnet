using Ably;
using Ably.Realtime;
using System;
using System.Windows;

namespace RealtimeChat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            payloadBox.Text = "{\"handle\":\"Your Name\",\"message\":\"Testing Message\"}";
        }

        private AblyRealtime client;
        private IRealtimeChannel channel;

        private void Subscribe_Click(object sender, RoutedEventArgs e)
        {
            string channelName = this.channelBox.Text.Trim();
            if (string.IsNullOrEmpty(channelName))
            {
                return;
            }

            string key = RealtimeChat.Properties.Settings.Default.ApiKey;
            AblyOptions options = new AblyOptions(key) { UseTextProtocol = true, Tls = true, AutoConnect = false };
            this.client = new AblyRealtime(options);
            this.client.Connection.ConnectionStateChanged += this.connection_ConnectionStateChanged;
            this.client.Connect();

            this.channel = this.client.Channels.Get(channelName);
            this.channel.ChannelStateChanged += channel_ChannelStateChanged;
            this.channel.MessageReceived += this.channel_MessageReceived;
            this.channel.Attach();
        }

        private void Trigger_Click(object sender, RoutedEventArgs e)
        {
            string eventName = this.eventBox.Text.Trim();
            string payload = this.payloadBox.Text.Trim();

            if (this.channel == null || string.IsNullOrEmpty(eventName))
            {
                return;
            }

            this.channel.Publish(eventName, payload);
        }

        private void connection_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            outputBox.Items.Add(string.Format("Connection: {0}", e.CurrentState));
        }

        private void channel_ChannelStateChanged(object sender, ChannelStateChangedEventArgs e)
        {
            outputBox.Items.Add(string.Format("Channel: {0}", e.NewState));
        }

        private void channel_MessageReceived(Message[] messages)
        {
            foreach (Message message in messages)
            {
                outputBox.Items.Add(string.Format("{0}: {1}", message.Name, message.Data));
            }
        }
    }
}
