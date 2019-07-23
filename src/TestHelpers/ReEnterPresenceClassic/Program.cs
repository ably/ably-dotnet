using System;
using System.Linq;
using System.Threading.Tasks;
using IO.Ably;
using IO.Ably.Realtime;

namespace ReEnterPresenceClassic
{
    class Program
    {
        private const string AblyKey = "oFpaLg.mB7oiw:WP5kW-Mrk96MTaFq";

        private static string _channelName = "channel-" + Guid.NewGuid().ToString().Substring(0, 8);

        private static IRealtimeChannel _channel;

        public static async Task Main(string[] args)
        {
            var opts = new ClientOptions(AblyKey)
            {
                LogHander = new ConsoleLogSink(),
                LogLevel = LogLevel.Debug,
                ClientId = $"client-{Guid.NewGuid().ToString().Substring(0, 8)}"
            };

            var ably = new AblyRealtime(opts);
            ably.Connect();
            ably.Connection.On(change =>
            {
                WriteLine($"ConnectionState changed to {change.Current}");
                if (change.Current == ConnectionState.Connected)
                {
                    GetPresence();
                }
            });

            WriteLine($"curl https://rest.ably.io/channels/{_channelName}/presence -u '{AblyKey}'");
            _channel = ably.Channels.Get(_channelName);

            _channel.On( state =>
            {
                WriteLine($"Channel State: {state.Current}");
                if (state.Current == ChannelState.Attached)
                {
                    GetPresence();
                }
            });

            _channel.Presence.Subscribe(message =>
            {
                WriteLine($"Presence Action: {message.Action}");
            });

            var result = await _channel.AttachAsync();
            if (result.IsSuccess)
            {
                await _channel.Presence.EnterAsync();
            }
            else
            {
                WriteLine("channel did not attach");
            }

            GetPresence();

            await Task.Run(() =>
            {
                Console.ReadLine();
                ably.Close();
            });
        }

        private static void GetPresence()
        {
            Task.Run(
                async () =>
                    {
                        try
                        {
                            WriteLine($"curl https://rest.ably.io/channels/{_channelName}/presence -u '{AblyKey}'");
                            var p = await _channel.Presence.GetAsync();
                            WriteLine($"Presence Count: {p.Count()}");
                            foreach (var presenceMessage in p)
                            {
                                WriteLine($"ClientId: {presenceMessage.ClientId}");
                            }
                        }
                        catch (Exception e)
                        {
                            WriteLine(e.ToString());
                        }

                    });
        }

        private static void WriteLine(string msg)
        {
            Console.WriteLine($"({DateTime.UtcNow.ToLongTimeString()}) {msg}");
        }

        class ConsoleLogSink : ILoggerSink
        {
            public void LogEvent(LogLevel level, string message)
            {
                WriteLine($"[{level}] {message}");
            }
        }
    }
}
