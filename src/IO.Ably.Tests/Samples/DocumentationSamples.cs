using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            byte[] key = Crypto.GenerateRandomKey();
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
            if (result.IsFailure)
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
            var query = new StatsDataRequestQuery() { Unit = StatsGranularity.Hour };
            var results = await realtime.StatsAsync(query);
            Stats thisHour = results.Items[0];
            Console.WriteLine("Published this hour " + thisHour.Inbound.All.All);
        }

        public async Task PresenceExample()
        {
            var options = new ClientOptions("{{API_KEY}}") { ClientId = "bob" };
            var realtime = new AblyRealtime(options);
            var channel = realtime.Channels.Get("{{RANDOM_CHANNEL_NAME}}");
            channel.Presence.Subscribe(member => Console.WriteLine("Member " + member.clientId + " : " + member.action));
            await channel.Presence.EnterAsync(null);

            /* Subscribe to presence enter and update events */
            channel.Presence.Subscribe(member =>
            {
                switch (member.action)
                {
                    case PresenceAction.Enter:
                    case PresenceAction.Update:
                        {
                            Console.WriteLine(member.data); // => travelling North
                            break;
                        }
                }
            });

            /* Enter this client with data and update once entered */
            await channel.Presence.EnterAsync("not moving");
            await channel.Presence.EnterAsync("travelling North");

            IEnumerable<PresenceMessage> presence = await channel.Presence.GetAsync();
            Console.WriteLine($"There are {presence.Count()} members on this channel");
            Console.WriteLine($"The first member has client ID: {presence.First().clientId}");

            PaginatedResult<PresenceMessage> resultPage = await channel.Presence.HistoryAsync(untilAttached: true);
            Console.WriteLine(resultPage.Items.Count + " presence events received in first page");
            if (resultPage.HasNext)
            {
                PaginatedResult<PresenceMessage> nextPage = await resultPage.NextAsync();
                Console.WriteLine(nextPage.Items.Count + " presence events received in 2nd page");
            }
        }

        public async Task PresenceExamples2()
        {
            /* request a wildcard token */
            AblyRest rest = new AblyRest("{{API_KEY}}");
            TokenParams @params = new TokenParams() { ClientId = "*" };
            ClientOptions options = new ClientOptions();
            options.TokenDetails = await rest.Auth.RequestTokenAsync(@params, null);

            AblyRealtime realtime = new AblyRealtime(options);
            var channel = realtime.Channels.Get("realtime-chat");

            channel.Presence.Subscribe(member =>
                    {
                        Console.WriteLine(member.clientId + " entered realtime-chat");
                    });

            await channel.Presence.EnterClientAsync("Bob", null); /* => Bob entered realtime-chat */
            await channel.Presence.EnterClientAsync("Mary", null); /* => Mary entered realtime-chat */
        }

        public async Task HistoryExamples()
        {
            var realtime = new AblyRealtime("{{API_KEY}}");
            var channel = realtime.Channels.Get("{{RANDOM_CHANNEL_NAME}}");
            channel.Publish("example", "message data", async (success, error) =>
            {
                PaginatedResult<Message> resultPage = await channel.HistoryAsync(null);
                Message lastMessage = resultPage.Items[0];
                Console.WriteLine("Last message: " + lastMessage.id + " - " + lastMessage.data);
            });
        }

        public async Task HistoryExample2()
        {
            var realtime = new AblyRealtime("{{API_KEY}}");
            var channel = realtime.Channels.Get("{{RANDOM_CHANNEL_NAME}}");
            await channel.AttachAsync();
            PaginatedResult<Message> resultPage = await channel.HistoryAsync(untilAttached: true);
            Message lastMessage = resultPage.Items[0];
            Console.WriteLine("Last message before attach: " + lastMessage.data);


            //Part of the _paginated_result sample
            PaginatedResult<Message> firstPage = await channel.HistoryAsync(null);
            Message firstMessage = firstPage.Items[0];
            Console.WriteLine("Page 0 item 0: " + firstMessage.data);
            if (firstPage.HasNext)
            {
                var nextPage = await firstPage.NextAsync();
                Console.WriteLine("Page 1 item 1:" + nextPage.Items[1].data);
                Console.WriteLine("More pages?: " + nextPage.HasNext);
            }
        }

        public async Task EncryptionExample()
        {
            var realtime = new AblyRealtime("{{API_KEY}}");
            var key = Crypto.GenerateRandomKey();
            var options = new ChannelOptions(key);
            var channel = realtime.Channels.Get("{{RANDOM_CHANNEL_NAME}}", options);
            channel.Subscribe(message =>
                {
                    Console.WriteLine("Decrypted data: " + message.data);
                });
            channel.Publish("unencrypted", "encrypted secret payload");


        }

        public async Task EncryptionExample2()
        {
            var @params = Crypto.GetDefaultParams();
            ChannelOptions options = new ChannelOptions(@params);
            var realtime = new AblyRealtime("{{API_KEY}}");
            var channel = realtime.Channels.Get("{{RANDOM_CHANNEL_NAME}}", options);


        }

        public async Task EncryptionExample3()
        {
            var realtime = new AblyRealtime("{{API_KEY}}");
            byte[] key = Crypto.GenerateRandomKey(keyLength: 128);
            ChannelOptions options = new ChannelOptions(key);
            var channel = realtime.Channels.Get("{{RANDOM_CHANNEL_NAME}}", options);
        }

        public async Task ConnectionExamples()
        {
            AblyRealtime realtime = new AblyRealtime("{{API_KEY}}");
            realtime.Connection.On(ConnectionState.Connected, args => Console.WriteLine("Connected, that was easy"));
            Action<ConnectionStateChangedEventArgs> action = args => Console.WriteLine("New state is " + args.Current);
            realtime.Connection.On(action);
            realtime.Connection.Off(action);

        }

        public void RestInit()
        {
            var rest = new AblyRest(new ClientOptions { AuthUrl = new Uri("https://my.website/auth") });
        }

        public async Task RestWithClientId()
        {
            var rest = new AblyRest(new ClientOptions {Key = "{{API_KEY}}"});
            var tokenParams = new TokenParams {ClientId = "Bob"};
            TokenRequest tokenRequest = await rest.Auth.CreateTokenRequestAsync(tokenParams);
            /* ... issue the TokenRequest to a client ... */
        }

        public async Task RestAuthorizeSample()
        {
            var client = new AblyRest("{{API_KEY}}");
            try
            {
                TokenParams tokenParams = new TokenParams {ClientId = "bob"};
                TokenDetails tokenDetails = await client.Auth.AuthoriseAsync(tokenParams);
                Console.WriteLine("Success; token = " + tokenDetails.Token);
            }
            catch (AblyException e)
            {
                Console.WriteLine("An error occurred; err = " + e.Message);
            }

            try
            {
                TokenParams tokenParams = new TokenParams { ClientId = "bob" };
                var tokenRequest = await client.Auth.CreateTokenRequestAsync(tokenParams);
                Console.WriteLine("Success; token request issued");
            }
            catch (AblyException e)
            {
                Console.WriteLine("An error occurred; err = " + e.Message);
            }

            try {
                TokenParams tokenParams = new TokenParams { ClientId = "bob" };
                var tokenDetails = await client.Auth.RequestTokenAsync(tokenParams);
                Console.WriteLine("Success; token = " + tokenDetails.Token);
            }
            catch (AblyException e)
            {
                Console.WriteLine("An error occurred; err = " + e.Message);
            }
        }

        public async Task RestChannelSamples()
        {
            AblyRest rest = new AblyRest("{{API_KEY}}");
            var channel = rest.Channels.Get("{{RANDOM_CHANNEL_NAME}}");
            await channel.PublishAsync("example", "message data");
            PaginatedResult<Message> resultPage = await channel.HistoryAsync();
            Console.WriteLine("Last published message ID: " + resultPage.Items[0].id);

            byte[] key = null;
            CipherParams cipherParams = Crypto.GetDefaultParams(key);
            ChannelOptions options = new ChannelOptions(cipherParams);
            var encryptedChannel = rest.Channels.Get("channelName", options);

            
        }

        public async Task RestChannelHistory()
        {
            AblyRest rest = new AblyRest("{{API_KEY}}");
            var channel = rest.Channels.Get("{{RANDOM_CHANNEL_NAME}}");

            PaginatedResult<Message> resultPage = await channel.HistoryAsync();
            Console.WriteLine(resultPage.Items.Count + " messages received in first page");
            if (resultPage.HasNext)
            {
                PaginatedResult<Message> nextPage = await resultPage.NextAsync();
                Console.WriteLine(nextPage.Items.Count + " messages received in second page");
            }
        }

        public async Task RestEncryption()
        {
            AblyRest rest = new AblyRest("{{API_KEY}}");
            var key = Crypto.GenerateRandomKey();
            ChannelOptions options = new ChannelOptions(key);
            var channel = rest.Channels.Get("{{RANDOM_CHANNEL_NAME}}", options);
            await channel.PublishAsync("unencrypted", "encrypted secret payload");

            CipherParams cipherParams = Crypto.GetDefaultParams(key);
            rest.Channels.Get("{{RANDOM_CHANNEL_NAME}}", new ChannelOptions(cipherParams));
        }

        public async Task RestGenerateRandomKey()
        {
            AblyRest rest = new AblyRest("{{API_KEY}}");
            byte[] key = Crypto.GenerateRandomKey(128);
            ChannelOptions options = new ChannelOptions(key);
            var channel = rest.Channels.Get("{{RANDOM_CHANNEL_NAME}}", options);
        }

        public async Task RestHistorySamples()
        {
            AblyRest rest = new AblyRest("{{API_KEY}}");
            var channel = rest.Channels.Get("{{RANDOM_CHANNEL_NAME}}");
            await channel.PublishAsync("example", "message data");
            PaginatedResult<Message> resultPage = await channel.HistoryAsync();
            Message recentMessage = resultPage.Items[0];
            Console.WriteLine("Most recent message: " + recentMessage.id + " - " + recentMessage.data);
        }

        public async Task RestPresenceSamples()
        {
            AblyRest rest = new AblyRest("{{API_KEY}}");
            var channel = rest.Channels.Get("{{RANDOM_CHANNEL_NAME}}");
            PaginatedResult<PresenceMessage> membersPage = await channel.Presence.GetAsync();
            Console.WriteLine(membersPage.Items.Count + " members in first page");
            if(membersPage.HasNext)
            {
                PaginatedResult<PresenceMessage> nextPage = await membersPage.NextAsync();
                Console.WriteLine(nextPage.Items.Count + " members on 2nd page");
            }

            //History
            PaginatedResult<PresenceMessage> eventsPage = await channel.Presence.HistoryAsync();
            Console.WriteLine(eventsPage.Items.Count + " presence events received in first page");
            if (eventsPage.HasNext)
            {
                PaginatedResult<PresenceMessage> nextPage = await eventsPage.NextAsync();
                Console.WriteLine(nextPage.Items.Count + " presence events received in 2nd page");
            }
        }

        public async Task RestStatsSamples()
        {
            AblyRest rest = new AblyRest("{{API_KEY}}");
            PaginatedResult<Stats> results = await rest.StatsAsync(new StatsDataRequestQuery() { Unit = StatsGranularity.Hour });
            Stats thisHour = results.Items[0];
            Console.WriteLine("Published this hour " + thisHour.Inbound.All.All.Count);
        }

        
    }
}
