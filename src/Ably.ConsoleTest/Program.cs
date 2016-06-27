using System;
using System.CodeDom;
using System.Linq;
using System.Threading;

namespace IO.Ably.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            IO.Ably.Logger.LoggerSink = new MyLogger();
            //Logger.LogLevel = LogLevel.Debug;
            try
            {
                //Rest.Test().Wait();
                var client = Realtime.Test();
                client.Connect(); 
                var channel = client.Channels.Get("testchannel0");
                channel.Attach();
                channel.Presence.Subscribe(Presence_MessageReceived2);
                channel.Presence.EnterClientAsync("clientid1", "mydata");

                while (true)
                {
                    channel.Publish(new Random().Next(1000000000, 1000000000).ToString(), new Random().Next(1000000000, 1000000000).ToString());
                    Thread.Sleep(1000);
                    Console.WriteLine("Bytes used: " + GC.GetTotalMemory(true));
                }

                //Console.ReadLine();
                //ConsoleColor.Green.WriteLine("Success!");
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