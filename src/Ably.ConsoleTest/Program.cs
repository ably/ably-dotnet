using System;

namespace IO.Ably.ConsoleTest
{
    class Program
    {
        static void Main( string[] args )
        {
            try
            {
                Rest.test().Wait();
                // Realtime.test().Wait();
                ConsoleColor.Green.writeLine( "Success!" );
            }
            catch( Exception ex )
            {
                ex.logError();
            }
        }
    }
}