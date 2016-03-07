using System;
using System.Threading.Tasks;

namespace IO.Ably.ConsoleTest
{
    class Program
    {

        static void Main( string[] args )
        {
            try
            {
                mainImpl().Wait();
                ConsoleColor.Green.writeLine( "Success!" );

            }
            catch( Exception ex )
            {
                ex.logError();
            }
        }

        static async Task mainImpl()
        {
            // throw new NotImplementedException();
        }
    }
}