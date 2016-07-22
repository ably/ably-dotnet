using System;
using System.Threading.Tasks;
using IO.Ably.Auth;
using IO.Ably.Encryption;
using IO.Ably.Realtime;
using IO.Ably.Rest;

namespace IO.Ably.Tests.Samples
{
    public class DocumentationSamples
    {
        public async Task AuthSamples1()
        {
            AblyRealtime realtime = new AblyRealtime("{{API_KEY}}");
            TokenParams tokenParams = new TokenParams { ClientId = "Bob" };
            TokenRequest tokenRequest = await realtime.Auth.CreateTokenRequestAsync(tokenParams);
            /* ... issue the TokenRequest to a client ... */
        }

        public async Task AuthSamples2()
        {
            AblyRealtime client = new AblyRealtime("{{API_KEY}}");
            try
            {
                TokenParams tokenParams = new TokenParams { ClientId = "bob" };
                TokenDetails tokenDetails = await client.Auth.AuthoriseAsync(tokenParams);
                Console.WriteLine("Success; Token = " + tokenDetails.Token);
            }
            catch (AblyException e)
            {
                Console.WriteLine("An error occurred; Error = " + e.Message);
            }
        }


        public async Task AuthSample3()
        {
            AblyRealtime client = new AblyRealtime("{{API_KEY}}");
            try
            {
                TokenParams tokenParams = new TokenParams { ClientId = "bob" };
                TokenRequest tokenRequest = await client.Auth.CreateTokenRequestAsync(tokenParams);
                Console.WriteLine("Success; token request issued");
            }
            catch (AblyException e)
            {
                Console.WriteLine("An error occurred; err = " + e.Message);
            }
        }

        public async Task AuthSample4()
        {
            AblyRealtime client = new AblyRealtime("{{API_KEY}}");

            try
            {
                TokenParams tokenParams = new TokenParams { ClientId = "bob" };
                TokenDetails tokenDetails = await client.Auth.RequestTokenAsync(tokenParams);
                Console.WriteLine("Success; token = " + tokenDetails.Token);
            }
            catch (AblyException e)
            {
                Console.WriteLine("An error occurred; err = " + e.Message);
            }
        }

        public void ChannelSample1()
        {
            AblyRealtime realtime = new AblyRealtime("{{API_KEY}}");
            var channel = realtime.Channels.Get("{{RANDOM_CHANNEL_NAME}}");
            channel.Subscribe(message =>
                        Console.WriteLine($"Message: {message.name}:{message.data} received")
                );
            channel.Publish("example", "message data");

            byte[] key = Crypto.GetRandomKey();
            var cipherParams = Crypto.GetDefaultParams(key);
            var encryptedChannel = realtime.Channels.Get("channelName", new ChannelOptions(cipherParams));
        }

        public void ChannelSample2()
        {
            AblyRealtime realtime = new AblyRealtime("{{API_KEY}}");
            var channel = realtime.Channels.Get("chatroom");
            channel.Attach((success, error) =>
            {
                Console.WriteLine("'chatroom' exists and is now available globally");
            });
        }

        public void ChannelSample3()
        {
            AblyRealtime realtime = new AblyRealtime("{{API_KEY}}");
            var channel = realtime.Channels.Get("chatroom");
            channel.Subscribe(message => Console.WriteLine("Message received:" + message.data));
            channel.Publish("action", "boom");
        }

        public async Task ChannelSample4()
        {
            AblyRealtime realtime = new AblyRealtime("{{API_KEY}}");
            var channel = realtime.Channels.Get("chatroom");
            channel.On(ChannelState.Attached, args => Console.WriteLine("channel " + channel.Name + " is now attached"));
            channel.On(args => Console.WriteLine("channel state is " + channel.State));

            Action<ChannelStateChangedEventArgs> channelStateListener = args => Console.WriteLine("channel state is " + channel.State);
            // remove the listener registered for a single event
            channel.Off(ChannelState.Attached, channelStateListener);

            // remove the listener registered for all events
            channel.Off(channelStateListener);

            var privateChannel = realtime.Channels.Get("private:chatroom");
            privateChannel.Attach((_, error) =>
            {
                if (error != null)
                {
                    Console.WriteLine("Attach failed: " + error.message);
                }
            });

            channel.Subscribe("myEvent", message =>
            {
                Console.WriteLine($"message received for event {message.name}");
                Console.WriteLine($"message data: {message.data}");
            });

            channel.Publish("event", "payload", (success, error) =>
            {
                if (error != null)
                    Console.WriteLine("Unable to publish message. Reason: " + error.message);
                else
                    Console.WriteLine("Message published sucessfully");
            });

            var result = await channel.PublishAsync("event", "payload");
            if(result.IsFailure)
                Console.WriteLine("Unable to publish message. Reason: " + result.Error.message);
            else
                Console.WriteLine("Message published sucessfully");

        }

        public async Task ChannelHistory()
        {
            AblyRealtime realtime = new AblyRealtime("{{API_KEY}}");
            var channel = realtime.Channels.Get("chatroom");
            var history = await channel.HistoryAsync(untilAttached: true);
            Console.WriteLine($"{history.Items.Count} messages received in the first page");
            if (history.HasNext)
            {
                var nextPage = await history.NextAsync();
            }
        }

        public async Task StatsExample()
        {
            var realtime = new AblyRealtime("{{API_KEY}}");
            var query = new StatsDataRequestQuery() { By = StatsBy.Hour };
            var results = await realtime.StatsAsync(query);
            Stats thisHour = results.items[0];
            Console.WriteLine("Published this hour " + thisHour.Inbound.All.All);

        }
    }
}
