using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            var data = File.ReadAllText("test16k.json");
            var message = JToken.Parse(data);
            try
            {
                //Rest.Test().Wait();
                var client = Realtime.Test();
                client.Connect();
                var channel = client.Channels.Get("Martin");
                await channel.AttachAsync();
                await channel.Presence.EnterAsync();
                client.Connection.On(changes =>
                {
                    ConsoleColor.Magenta.WriteLine($"Connection: {changes.Current} from {changes.Previous}. Error: {(changes.HasError ? changes.Reason.Message : "")}");
                });
                int count = 0;
                int succeeded = 0;
                while (true)
                {
                    channel.Publish(new Random().Next(1000000000, 1000000000).ToString(), message, (s, error) =>
                    {
                        if (s == false)
                            ConsoleColor.Red.WriteLine("Error sending message. Error: " + error);
                        else
                            succeeded++;
                    });
                    await Task.Delay(100);
                    count++;
                    if(count % 10 == 0)
                        ConsoleColor.Green.WriteLine("Messages sent: " + count + ". Succeeded: " + succeeded);
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