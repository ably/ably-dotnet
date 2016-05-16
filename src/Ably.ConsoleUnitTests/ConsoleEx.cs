using System;
using System.Linq;

namespace IO.Ably.ConsoleTest
{
    static class ConsoleEx
    {
        static readonly object syncRoot = new object();

        public static void WriteLine(this ConsoleColor cc, string message)
        {
            if (silent)
                return;
            lock (syncRoot)
            {
                ConsoleColor oc = Console.ForegroundColor;
                Console.ForegroundColor = cc;
                Console.WriteLine(message);
                Console.ForegroundColor = oc;
            }
        }

        public static void WriteLine(this ConsoleColor cc, string message, params object[] args)
        {
            if (silent)
                return;
            lock (syncRoot)
            {
                ConsoleColor oc = Console.ForegroundColor;
                Console.ForegroundColor = cc;
                Console.WriteLine(message, args);
                Console.ForegroundColor = oc;
            }
        }

        public static void Write(this ConsoleColor cc, string message)
        {
            if (silent)
                return;
            lock (syncRoot)
            {
                ConsoleColor oc = Console.ForegroundColor;
                Console.ForegroundColor = cc;
                Console.Write(message);
                Console.ForegroundColor = oc;
            }
        }

        public static void LogError(this Exception ex)
        {
            if (ex is AggregateException)
                ex = (ex as AggregateException).Flatten().InnerExceptions.First();
            lock (syncRoot)
            {
                WriteLine(ConsoleColor.Red, ex.Message);
                WriteLine(ConsoleColor.DarkRed, ex.ToString());
            }
        }

        public static bool silent = false;
    }
}