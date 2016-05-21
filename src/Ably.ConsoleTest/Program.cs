using System;
using System.CodeDom;

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

                Console.ReadLine();
                ConsoleColor.Green.WriteLine("Success!");
            }
            catch (Exception ex)
            {
                ex.LogError();
            }
        }
    }
}