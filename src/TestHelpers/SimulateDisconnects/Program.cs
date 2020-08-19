using System;
using System.Threading.Tasks;
using IO.Ably;
using IO.Ably.Realtime;

namespace SimulateDisconnects
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            var authRestClient = new AblyRest("oFpaLg.mB7oiw:WP5kW-Mrk96MTaFq");
            var options = new ClientOptions("oFpaLg.mB7oiw:WP5kW-Mrk96MTaFq")
            {
                LogLevel = LogLevel.Debug,
                LogHander = new ConsoleLogger(),
                DisconnectedRetryTimeout = TimeSpan.FromSeconds(3),
                AuthCallback = async tokenParams => await authRestClient.Auth.RequestTokenAsync(new TokenParams { Ttl = TimeSpan.FromSeconds(2) }),
            };

            var client = new AblyRealtime(options);

            client.Connection.On(ConnectionEvent.Connected, change =>
            {
                Console.WriteLine("client connected");
                var channel = client.Channels.Get("test-channel");
                channel.Subscribe(message =>
                {
                    Console.WriteLine("channel subscribed");
                });
                channel.Presence.EnterClientAsync("test-id", null);
            });

            client.Connection.On(ConnectionEvent.Disconnected, change =>
            {
                Console.WriteLine("client disconnected");
            });

            Console.ReadLine();
        }
    }

    public class ConsoleLogger : ILoggerSink
    {
        public void LogEvent(LogLevel level, string message)
        {
            Console.WriteLine($"[{level}] {message}");
        }
    }
}
