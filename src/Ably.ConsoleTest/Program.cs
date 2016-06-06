using System;
using System.CodeDom;
using System.Linq;

namespace IO.Ably.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            IO.Ably.Logger.LoggerSink = new MyLogger();
            Logger.LogLevel = LogLevel.Debug;
            try
            {
                //Rest.Test().Wait();
                var client = Realtime.Test();
                client.Connect(); 
                var channel = client.Get("testchannel0");
                channel.Attach();
                channel.Presence.Subscribe(Presence_MessageReceived2);
                channel.Presence.EnterClient("clientid1", "mydata");

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
            Console.WriteLine(obj.connectionId + "\t" + obj.timestamp);
        }
    }
}