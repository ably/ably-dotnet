using System;

namespace IO.Ably.ConsoleTest
{
    static class ConsoleEx
    {
        static readonly object syncRoot = new object();

        public static void writeLine( ConsoleColor cc, string message )
        {
            if( silent )
                return;
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
            if( silent )
                return;
            lock ( syncRoot )
            {
                ConsoleColor oc = Console.ForegroundColor;
                Console.ForegroundColor = cc;
                Console.WriteLine( message, args );
                Console.ForegroundColor = oc;
            }
        }

        public static void write( ConsoleColor cc, string message )
        {
            if( silent )
                return;
            lock ( syncRoot )
            {
                ConsoleColor oc = Console.ForegroundColor;
                Console.ForegroundColor = cc;
                Console.Write( message );
                Console.ForegroundColor = oc;
            }
        }

        public static bool silent = false;
    }
}