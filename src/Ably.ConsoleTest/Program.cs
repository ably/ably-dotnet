using Ably.AcceptanceTests;
using System.Collections.Generic;
using System.Reflection;

namespace Ably.ConsoleTest
{
    class Program
    {
        static void Main( string[] args )
        {
            Assembly ass = typeof(LoggerTests).Assembly;
            string path = ass.Location;

            List<string> options = new List<string>();
            options.Add( path );
            options.Add( "/domain:None" );
            options.Add( "/nothread" );
            options.Add( "/nologo" );
            options.Add( "/labels" );
            options.Add( "/stoponerror" );

            NUnit.ConsoleRunner.Runner.Main( options.ToArray() );
        }
    }
}