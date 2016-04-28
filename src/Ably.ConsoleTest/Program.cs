﻿using System;

namespace IO.Ably.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            IO.Ably.Logger.LoggerSink = new MyLogger();
            try
            {
                Rest.Test().Wait();
                // Realtime.test().Wait();
                ConsoleColor.Green.writeLine("Success!");
            }
            catch (Exception ex)
            {
                ex.logError();
            }
        }
    }
}