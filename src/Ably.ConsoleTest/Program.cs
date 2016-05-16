using System;

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