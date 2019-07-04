using System;

namespace ReEnterPresence
{
    using System.Linq;
    using System.Threading.Tasks;

    using IO.Ably;
    using IO.Ably.Realtime;

    class Program
    {
        private const string AblyKey = "oFpaLg.mB7oiw:WP5kW-Mrk96MTaFq";

        private static IRealtimeChannel _channel;

        public static async Task Main(string[] args)
        {
            var opts = new ClientOptions(AblyKey);
            opts.ClientId = Guid.NewGuid().ToString().Substring(0, 8);
            var ably = new AblyRealtime(opts);
            ably.Connect();
            ably.Connection.On(change => Console.WriteLine($"ConnectionState changed to {change.Current}"));

            _channel = ably.Channels.Get("foo_" + Guid.NewGuid().ToString().Substring(0,8));

            _channel.On(ChannelEvent.Attached,_ => LogPresence());

            var result = await _channel.AttachAsync();
            if (result.IsSuccess)
            {
                await _channel.Presence.EnterAsync();
            }
            else
            {
                Console.WriteLine("channel did not attach");
            }

            LogPresence();

            Console.ReadLine();
            ably.Close();
        }

        private static void LogPresence()
        {
            Task.Run(
                async () =>
                    {
                        var p = await _channel.Presence.GetAsync();
                        Console.WriteLine(p.Count());
                    });
        }
    }
}
