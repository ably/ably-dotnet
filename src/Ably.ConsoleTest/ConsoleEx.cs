using System;

namespace Ably.ConsoleTest
{
    static class ConsoleEx
    {
        static readonly object syncRoot = new object();

        public static void writeLine( ConsoleColor cc, string message )
        {
            lock ( syncRoot )
            {
                ConsoleColor oc = Console.ForegroundColor;
                Console.ForegroundColor = cc;
                Console.WriteLine( message );
                Console.ForegroundColor = oc;
            }
        }

        public static void writeLine( ConsoleColor cc, string message, params object[] args )
        {
            lock ( syncRoot )
            {
                ConsoleColor oc = Console.ForegroundColor;
                Console.ForegroundColor = cc;
                Console.WriteLine( message, args );
                Console.ForegroundColor = oc;
            }
        }
    }
}