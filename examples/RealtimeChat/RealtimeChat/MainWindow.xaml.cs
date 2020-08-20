using IO.Ably;
using IO.Ably.Realtime;
using System;
using System.Threading;
using System.Windows;
using IO.Ably.Rest;
using System.Threading.Tasks;

namespace RealtimeChat
{
    public class PresenceData
    {
        public string[] Data { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        class CustomLoggerSink : ILoggerSink
        {
            private readonly Action<string> _logEvent;

            public CustomLoggerSink(Action<string> logEvent)
            {
                _logEvent = logEvent;
            }

            public void LogEvent(LogLevel level, string message)
            {
                _logEvent($"{level}:{message}");
            }
        }

        public MainWindow()
        {
            InitializeComponent();


            payloadBox.Text = "{\"handle\":\"Your Name\",\"message\":\"Testing Message\"}";
        }

        private AblyRealtime client;
        private IRealtimeChannel channel;

        private SynchronizationContext Context;

        /// DO NOT CONSIDER THIS AS BEST PRACTISE
        /// IT's USED FOR MANUAL TESTING TO CATCH EDGE CASES
        private async void Subscribe_Click(object sender, RoutedEventArgs e)
        {
            Context = SynchronizationContext.Current;

            string channelName = this.channelBox.Text.Trim();
            if (string.IsNullOrEmpty(channelName))
            {
                return;
            }

            string key = RealtimeChat.Properties.Settings.Default.ApiKey;
            string clientId = RealtimeChat.Properties.Settings.Default.ClientId;
            var options = new ClientOptions(key)
            {
                UseBinaryProtocol = false,
                Tls = true,
                AutoConnect = false,
                ClientId = clientId,
                LogLevel = LogLevel.Debug,
                LogHandler = new CustomLoggerSink(message => Context.Post(state => logBox.Items.Add(message), null))
            };
            this.client = new AblyRealtime(options);
            this.client.Connection.ConnectionStateChanged += this.connection_ConnectionStateChanged;
            this.client.Connect();

            this.channel = this.client.Channels.Get(channelName);
            this.channel.StateChanged += channel_ChannelStateChanged;
            this.channel.Subscribe(Handler);
            this.channel.Presence.Subscribe(Presence_MessageReceived);
            try
            {
                await channel.AttachAsync();
                await channel.Presence.EnterAsync(new PresenceData() { Data = new[] { "data1", "data2" } });
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void Handler(Message message)
        {
            PostAction(() => eventsBox.Items.Add($"Message: {message}"));
        }

        public void PostAction(Action action)
        {
            Context?.Post((a) => action(), null);
        }

        private void Trigger_Click(object sender, RoutedEventArgs e)
        {
            string eventName = this.eventBox.Text.Trim();
            string payload = this.payloadBox.Text.Trim();

            if (this.channel == null || string.IsNullOrEmpty(eventName))
            {
                return;
            }

            channel.PublishAsync(eventName, payload);
        }

        private void PresenceCurrent_Click(object sender, RoutedEventArgs e)
        {
            channel.Presence.Enter();
        }

        private void PresenceCurrentLeave_Click(object sender, RoutedEventArgs e)
        {
            channel.Presence.Leave();
        }

        private void Presence_Click(object sender, RoutedEventArgs e)
        {
            string eventName = this.eventBox.Text.Trim();
            string payload = this.payloadBox.Text.Trim();

            if (this.channel == null || string.IsNullOrEmpty(eventName))
            {
                return;
            }

            channel.Presence.EnterClient(eventName, payload);
        }

        private void PresenceLeave_Click(object sender, RoutedEventArgs e)
        {
            string eventName = this.eventBox.Text.Trim();
            string payload = this.payloadBox.Text.Trim();

            if (this.channel == null || string.IsNullOrEmpty(eventName))
            {
                return;
            }

            channel.Presence.LeaveClient(eventName, payload, (success, error) =>
            PostAction(() => eventsBox.Items.Add($"Presence Leave {success} with error {error}")));
        }

        private void connection_ConnectionStateChanged(object sender, ConnectionStateChange e)
        {

            PostAction(() => eventsBox.Items.Add(string.Format("Connection: {0}", e.Current)));
        }

        private void channel_ChannelStateChanged(object sender, ChannelStateChange e)
        {
            PostAction(() => eventsBox.Items.Add(string.Format("Channel: {0}", e.Current)));
        }

        void Presence_MessageReceived(PresenceMessage message)
        {
            _ = OnPresenceMessage(message);
        }

        async Task OnPresenceMessage(PresenceMessage message)
        {
            var presence = await channel.Presence.GetAsync(true);
            PostAction(() =>
            {
                presenceBox.Items.Clear();
                foreach(var p in presence)
                {
                    if(p.Action == PresenceAction.Enter || p.Action == PresenceAction.Present)
                    {
                        presenceBox.Items.Add($"{p.ClientId}:{p.Action}. Data: {p.Data}");
                    }
                }
            });
            PostAction(() => eventsBox.Items.Add(string.Format("{0}: {1} {2}", message.Data, message.Action, message.ClientId)));
        }
    }
}
