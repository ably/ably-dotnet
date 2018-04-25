using System.Linq;
using System.Threading.Tasks;
using IO.Ably.Encryption;
using IO.Ably.Realtime;
using Xunit;

namespace IO.Ably.Tests.GithubSamples
{
    public class RealtimeSamples
    {
        private string _placeholderKey = "key.placeholder:placeholder";

        [Fact]
        public void InitialiseClient()
        {
            // If you do not have an API key, [sign up for a free API key now](ht tps://www.ably.io/signup)
            var realtimeBasic = new AblyRealtime(_placeholderKey);
            var realtimeToken = new AblyRealtime(new ClientOptions { Token = "token" });
        }

        [Fact]
        public void SuccessfulConnection()
        {
            var realtime = new AblyRealtime(_placeholderKey);

            realtime.Connection.On(ConnectionEvent.Connected, args =>
            {
                // Do stuff
            });

            // or
            realtime.Connection.ConnectionStateChanged += (s, args) =>
            {
                if (args.Current == ConnectionState.Connected)
                {
                    // Do stuff
                }
            };
        }

        [Fact]
        public void AutoConnectOff()
        {
            var realtime = new AblyRealtime(new ClientOptions(_placeholderKey) { AutoConnect = false });
            realtime.Connect();

            realtime.Connection.On(args =>
            {
                var currentState = args.Current; // Current state the connection transitioned to
                var previousState = args.Previous; // Previous state
                var error = args.Reason; // If the connection errored the Reason object will be populated.
            });
        }

        [Fact(Skip = "Used to make sure samples compile")]
        public async Task ChannelSubscribe()
        {
            var realtime = new AblyRealtime(new ClientOptions(_placeholderKey) { AutoConnect = false });
            var channel = realtime.Channels.Get("test");

            // or
            var channel2 = realtime.Channels.Get("shortcut");

            channel.Subscribe(message =>
            {
                var name = message.Name;
                var data = message.Data;
            });

            channel.On(args =>
            {
                var state = args.Current; // Current channel State
                var error = args.Error; // If the channel errored it will be refrected here
            });

            channel.On(ChannelState.Attached, args =>
            {
                // Do stuff when channel is attached
            });

            channel.Publish("greeting", "Hello World!");
            channel.Publish("greeting", "Hello World!", (success, error) =>
            {
                // if publish succeeded `success` is true
                // if publish failed `success` is false and error will contain the specific error
            });

            var result = await channel.PublishAsync("greeting", "Hello World!");

            // You can check if the message failed
            if (result.IsFailure)
            {
                var error = result.Error; // The error reason can be accessed as well
            }

            var secret = Crypto.GenerateRandomKey();
            var encryptedChannel = realtime.Channels.Get("encrypted", new ChannelOptions(secret));
            encryptedChannel.Subscribe(message =>
            {
                var data = message.Data; // sensitive data (encrypted before published)
            });
            encryptedChannel.Publish("name (not encrypted)", "sensitive data (encrypted before published)");
        }

        [Fact(Skip = "Just need to make sure it compiles")]
        public async Task ChannelHistory()
        {
            var realtime = new AblyRealtime(_placeholderKey);
            var channel = realtime.Channels.Get("test");
            var history = await channel.HistoryAsync();

            // loop through current history page
            foreach (var message in history.Items)
            {
                // Do something with message
            }

            var nextPage = await history.NextAsync();

            var presenceHistory = await channel.Presence.HistoryAsync();

            // loop through the presence messages
            foreach (var presence in presenceHistory.Items)
            {
                // Do something with the messages
            }

            var presenceNextPage = await presenceHistory.NextAsync();
        }

        [Fact(Skip = "Making sure the samples compile")]
        public async Task RestApiSamples()
        {
            var client = new AblyRest(_placeholderKey);
            var channel = client.Channels.Get("test");

            try
            {
                await channel.PublishAsync("name", "data");
            }
            catch (AblyException)
            {
                // Log error
            }

            // History
            var historyPage = await channel.HistoryAsync();
            foreach (var message in historyPage.Items)
            {
                // Do something with each message
            }

            // get next page
            var nextHistoryPage = await historyPage.NextAsync();

            // Current presence
            var presence = await channel.Presence.GetAsync();
            var first = presence.Items.FirstOrDefault();
            var clientId = first.ClientId; // clientId of the first member present
            var nextPresencePage = await presence.NextAsync();
            foreach (var presenceMessage in nextPresencePage.Items)
            {
                // do stuff with next page presence messages
            }

            // Presence history
            var presenceHistory = await channel.Presence.HistoryAsync();
            foreach (var presenceMessage in presenceHistory.Items)
            {
                // Do stuff with presence messages
            }

            var nextPage = await presenceHistory.NextAsync();
            foreach (var presenceMessage in nextPage.Items)
            {
                // Do stuff with next page messages
            }

            // publishing encrypted messages
            var secret = Crypto.GenerateRandomKey();
            var encryptedChannel = client.Channels.Get("encryptedChannel", new ChannelOptions(secret));
            await encryptedChannel.PublishAsync("name", "sensitive data"); // Data will be encrypted before publish
            var history = await encryptedChannel.HistoryAsync();
            var data = history.Items.First().Data;

            // "sensitive data" the message will be automatically decrypted once received

            // Generate a token
            var token = await client.Auth.RequestTokenAsync();
            var tokenString = token.Token; // "xVLyHw.CLchevH3hF....MDh9ZC_Q"
            var tokenClient = new AblyRest(new ClientOptions { TokenDetails = token });

            var tokenRequest = await client.Auth.CreateTokenRequestAsync();

            // Stats
            var stats = await client.StatsAsync();
            var firstItem = stats.Items.First();
            var nextStatsPage = await stats.NextAsync();

            var time = await client.TimeAsync();
        }
    }
}
