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
            //IO.Ably.Logger.LoggerSink = new MyLogger();
            try
            {
                //Rest.Test().Wait();
                var client = Realtime.Test();
                client.Connect(); 
                var channel = client.Channels.Get("test");
                await channel.AttachAsync();
                DateTime start = DateTime.Now;
                while (true)
                {
                    channel.Publish(new Random().Next(1000000000, 1000000000).ToString(), new Random().Next(1000000000, 1000000000).ToString());
                    Thread.Sleep(1000);
                    Console.WriteLine("Connected time: " + (DateTime.Now - start).TotalSeconds + " seconds");
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