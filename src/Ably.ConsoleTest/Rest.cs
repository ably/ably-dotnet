using IO.Ably.Rest;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IO.Ably.ConsoleTest
{
    static class Rest
    {
        public static async Task Test()
        {
            ConsoleColor.DarkGreen.WriteLine("Creating sandbox app..");

            ConsoleColor.DarkGreen.WriteLine("Creating REST client..");

            // Create REST client using that key
            AblyRest ably = new AblyRest(new ClientOptions()
            {
                AuthUrl = new Uri("https://www.ably.io/ably-auth/token-request/demos"),
                Tls = false
            });

            ConsoleColor.DarkGreen.WriteLine("Publishing a message..");

            // Verify we can publish
            IChannel channel = ably.Channels.Get("persisted:presence_fixtures");

            var tsPublish = DateTimeOffset.UtcNow;
            await channel.PublishAsync("test", true);

            ConsoleColor.DarkGreen.WriteLine("Getting the history..");

            PaginatedResult<Message> history = await channel.HistoryAsync();

            if (history.Count <= 0)
                throw new ApplicationException("Message lost: not on the history");

            Message msg = history.First();
            var tsNow = DateTimeOffset.UtcNow;
            var tsHistory = msg.timestamp.Value;

            if (tsHistory < tsPublish)
                throw new ApplicationException("Timestamp's too early. Please ensure your PC's time is correct, use e.g. time.nist.gov server.");
            if (tsHistory > tsNow)
                throw new ApplicationException("Timestamp's too late, Please ensure your PC's time is correct, use e.g. time.nist.gov server.");

            ConsoleColor.DarkGreen.WriteLine("Got the history");
        }
    }
}