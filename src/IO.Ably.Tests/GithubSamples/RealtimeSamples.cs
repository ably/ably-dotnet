using System.Linq;
using System.Threading.Tasks;
using IO.Ably.Encryption;
using IO.Ably.Realtime;
using IO.Ably.Rest;
using IO.Ably.Tests.Rest;
using Xunit;

namespace IO.Ably.Tests.GithubSamples
{
    public class RealtimeSamples
    {
        private string placeholderKey = "key.placeholder:placeholder";

        [Fact]
        public void InitialiseClient()
        {
            //If you do not have an API key, [sign up for a free API key now](ht tps://www.ably.io/signup)
            var realtimeBasic = new AblyRealtime(placeholderKey);
            var realtimeToken = new AblyRealtime(new ClientOptions { Token = "token" });
        }

        [Fact]
        public void SuccessfulConnection()
        {
            var realtime = new AblyRealtime(placeholderKey);
            realtime.Connection.ConnectionStateChanged += (s, args) =>
            {
                if (args.CurrentState == ConnectionStateType.Connected)
                {
                    // Do stuff
                }
            };
        }

        [Fact]
        public void AutoConnectOff()
        {
            var realtime = new AblyRealtime(new ClientOptions(placeholderKey) { AutoConnect = false });
            realtime.Connect();
            realtime.Connection.ConnectionStateChanged += (s, args) =>
            {
                var currentState = args.CurrentState; //Current state the connection transitioned to
                var previousState = args.PreviousState; // Previous state
                var error = args.Reason; // If the connection errored the Reason object will be populated.
            };
        }

        [Fact(Skip = "Used to make sure samples compile")]
        public async Task ChannelSubscribe()
        {
            var realtime = new AblyRealtime(new ClientOptions(placeholderKey) { AutoConnect = false });
            var channel = realtime.Channels.Get("test");
            //or
            var channel2 = realtime.Channels.Get("shortcut");

            channel.Subscribe(message =>
            {
                var name = message.name;
                var data = message.data;
            });

            channel.StateChanged += (s, args) =>
            {
                var state = args.NewState; //Current channel State
                var error = args.Reason; // If the channel errored it will be refrected here

                if (state == ChannelState.Attached)
                {
                    // Do stuff
                }
            };

            channel.Publish("greeting", "Hello World!");
            channel.Publish("greeting", "Hello World!", (success, error) =>
            {
                //if publish succeeded `success` is true
                //if publish failed `success` is false and error will contain the specific error
            });

            var result = await channel.PublishAsync("greeting", "Hello World!");

            var secret = Crypto.GetRandomKey();
            var encryptedChannel = realtime.Channels.Get("encrypted", new ChannelOptions(secret));
            encryptedChannel.Subscribe(message =>
            {
                var data = message.data; // sensitive data (encrypted before published)
            });
            encryptedChannel.Publish("name (not encrypted)", "sensitive data (encrypted before published)");
        }

        [Fact(Skip = "Just need to make sure it compiles")]
        public async Task ChannelHistory()
        {
            var realtime = new AblyRealtime(placeholderKey);
            var channel = realtime.Channels.Get("test");
            var history = await channel.HistoryAsync();
            var firstMessage = history.FirstOrDefault();
            //loop through current history page
            foreach (var message in history)
            {
                //Do something with message
            }
            //check if next page exists and get it
            if (history.HasNext)
            {
                var nextPage = await channel.HistoryAsync(history.NextQuery);
                //Do stuff with next page
            }

            var presenceHistory = await channel.Presence.HistoryAsync();
            var firstPresenceMessage = presenceHistory.FirstOrDefault();
            //loop through the presence messages
            foreach (var presence in presenceHistory)
            {
                //Do something with the messages
            }
            //check if next page exists and get it
            if (presenceHistory.HasNext)
            {
                var nextPage = await channel.Presence.HistoryAsync(presenceHistory.NextQuery);
                //Continue work with the next page
            }

        }

        [Fact(Skip = "Making sure the samples compile")]
        public async Task RestApiSamples()
        {
            var client = new AblyRest(placeholderKey);
            var channel = client.Channels.Get("test");

            try
            {
                await channel.PublishAsync("name", "data");
            }
            catch (AblyException)
            {
                // Log error
            }

            //History
            var historyPage = await channel.HistoryAsync();
            var firstMessage = historyPage.FirstOrDefault();
            foreach (var message in historyPage)
            {
                //Do something with each message
            }
            //get next page if there is one
            if (historyPage.HasNext)
            {
                var nextPage = await channel.HistoryAsync(historyPage.NextQuery);
            }

            //Current presence
            var presence = await channel.Presence.GetAsync();
            var first = presence.FirstOrDefault();
            var clientId = first.clientId; //clientId of the first member present
            if (presence.HasNext)
            {
                var nextPage = await channel.Presence.GetAsync(presence.NextQuery);
                foreach (var presenceMessage in nextPage)
                {
                    //do stuff with next page presence messages
                }
            }

            // Presence history
            var presenceHistory = await channel.Presence.HistoryAsync();
            var firstHistoryMessage = presenceHistory.FirstOrDefault();
            foreach (var presenceMessage in presenceHistory)
            {
                // Do stuff with presence messages
            }

            if (presenceHistory.HasNext)
            {
                var nextPage = await channel.Presence.HistoryAsync(presenceHistory.NextQuery);
                foreach (var presenceMessage in nextPage)
                {
                    // Do stuff with next page messages
                }
            }

            // publishing encrypted messages
            var secret = Crypto.GetRandomKey();
            var encryptedChannel = client.Channels.Get("encryptedChannel", new ChannelOptions(secret));
            await encryptedChannel.PublishAsync("name", "sensitive data"); //Data will be encrypted before publish
            var history = await encryptedChannel.HistoryAsync();
            var data = history.First().data;
            // "sensitive data" the message will be automatically decrypted once received

            //Generate a token
            var token = await client.Auth.RequestTokenAsync();
            var tokenString = token.Token; // "xVLyHw.CLchevH3hF....MDh9ZC_Q"
            var tokenClient = new AblyRest(new ClientOptions {TokenDetails = token});

            var tokenRequest = await client.Auth.CreateTokenRequestAsync();

            //Stats
            var stats = await client.StatsAsync();
            var firstItem = stats.First();
            if (stats.HasNext)
            {
                var nextPage = await client.StatsAsync(stats.NextQuery);
            }

            var time = await client.TimeAsync();
        }
    }
}
