using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Ably.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            IO.Ably.DefaultLogger.LoggerSink = new MyLogger();
            DefaultLogger.LogLevel = LogLevel.Warning;
            try
            {
                //Rest.Test().Wait();
                var client = Realtime.Test();
                client.Connect(); 
                var channel = client.Channels.Get("Martin");
                await channel.AttachAsync();
                await channel.Presence.EnterAsync();
                DateTime start = DateTime.Now;
                while (true)
                {
                    channel.Publish(new Random().Next(1000000000, 1000000000).ToString(), new Random().Next(1000000000, 1000000000).ToString(), (s, error) => Console.WriteLine("Message sent: " + s));
                    await Task.Delay(1000);
                    Console.WriteLine("Connected time: " + (DateTime.Now - start).TotalSeconds + " seconds.");
                }

                Console.ReadLine();
                ConsoleColor.Green.WriteLine("Success!");
            }
            catch (Exception ex)
            {
                ex.LogError();
            }
        }

        private static void Presence_MessageReceived2(PresenceMessage obj)
        {
            Console.WriteLine(obj.ConnectionId + "\t" + obj.Timestamp);
        }
    }
}