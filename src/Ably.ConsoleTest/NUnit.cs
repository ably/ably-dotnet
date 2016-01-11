using System.Collections.Generic;
using System.Reflection;

namespace Ably.ConsoleTest
{
    static class NUnit
    {
        public static void Run( Assembly ass, bool stopOnErrors, bool printLabels, bool logToConsole, string singleTest = null )
        {
            if( logToConsole )
                Logger.SetDestination( new MyLogger() );

            string path = ass.Location;

            List<string> options = new List<string>();
            // http://www.nunit.org/index.php?p=consoleCommandLine&r=2.6.3

            if( null != singleTest )
                options.Add( "/run:" + singleTest );

            options.Add( path );
            options.Add( "/domain:None" );
            options.Add( "/nothread" );
            options.Add( "/nologo" );
            if( printLabels )
                options.Add( "/labels" );
            if( stopOnErrors )
                options.Add( "/stoponerror" );

            global::NUnit.ConsoleRunner.Runner.Main( options.ToArray() );
        }
    }
}