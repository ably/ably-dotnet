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
        static void RunTests( Assembly ass, bool stopOnErrors, bool printLabels, bool logToConsole, string singleTest = null )
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

            NUnit.ConsoleRunner.Runner.Main( options.ToArray() );
        }

        static void Main( string[] args )
        {
            Assembly ass = typeof(LoggerTests).Assembly;
            // Run all of them, with brief output
            RunTests( ass, false, false, false );

            // Run all of them, with verbose output, and stop on errors
            // RunTests( ass, true, true, true );

            // Run the single test
            // RunTests( ass, true, true, true, "Ably.AcceptanceTests.RealtimeAcceptanceTests(MsgPack).TestCreateRealtimeClient_AutoConnect_False_ConnectsSuccessfuly" );
        }
    }
}