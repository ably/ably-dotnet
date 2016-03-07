using System;
using System.Linq;

namespace IO.Ably.ConsoleTest
{
    static class ConsoleEx
    {
        static readonly object syncRoot = new object();

        public static void writeLine( this ConsoleColor cc, string message )
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

        public static void writeLine( this ConsoleColor cc, string message, params object[] args )
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

        public static void write( this ConsoleColor cc, string message )
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

        public static void logError( this Exception ex )
        {
            if( ex is AggregateException )
                ex = ( ex as AggregateException ).Flatten().InnerExceptions.First();
            lock ( syncRoot )
            {
                writeLine( ConsoleColor.Red, ex.Message );
                writeLine( ConsoleColor.DarkRed, ex.ToString() );
            }
        }

        public static bool silent = false;
    }
}