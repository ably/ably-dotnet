using Ably.AcceptanceTests;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace Ably.ConsoleTest
{
    class MyLogger : ILoggerSink
    {
        readonly object syncRoot = new object();

        static readonly Dictionary<LogLevel, ConsoleColor> s_colors = new Dictionary<LogLevel, ConsoleColor>()
        {
            { LogLevel.Error, ConsoleColor.DarkRed },
            { LogLevel.Warning, ConsoleColor.DarkYellow },
            { LogLevel.Info, ConsoleColor.DarkGreen },
            { LogLevel.Debug, ConsoleColor.DarkBlue },
        };

        void ILoggerSink.LogEvent( LogLevel level, string message )
        {
            ConsoleColor cc = s_colors[ level ];
            lock ( syncRoot )
            {
                ConsoleColor oc = Console.ForegroundColor;
                Console.ForegroundColor = cc;
                Console.WriteLine( message );
                Console.ForegroundColor = oc;
            }
        }
    }

    class Program
    {
        static void Main( string[] args )
        {
            Logger.SetDestination( new MyLogger() );

            Assembly ass = typeof(LoggerTests).Assembly;
            string path = ass.Location;

            List<string> options = new List<string>();
            // http://www.nunit.org/index.php?p=consoleCommandLine&r=2.6.3

            // options.Add( "/run:Ably.AcceptanceTests.RealtimeAcceptanceTests(MsgPack).TestCreateRealtimeClient_AutoConnect_False_ConnectsSuccessfuly" );

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