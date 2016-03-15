using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Xunit;
using Xunit.Abstractions;
using Xunit.Runners;

namespace Ably.ConsoleTest
{
    /// <summary>A static class that runs XUnit tests from the specified assembly, and outputs result to console.</summary>
    static class XUnit
    {
        class Runner : IDisposable
        {
            readonly AssemblyRunner impl;
            readonly ManualResetEvent completed = new ManualResetEvent( false );

            public bool stopOnErrors = false;
            public bool verbose = true;
            public string strTest = null;

            public Runner( AssemblyRunner ar )
            {
                impl = ar;

                // Perfect example why you should never use delegates for more then 2-3 events, if you expect your user will need to handle many of them.
                // An interface or abstract class would be more appropiriate here.

                ar.OnExecutionComplete = this.ExecutionComplete;

                ar.OnTestStarting = this.TestStarting;
                ar.OnTestFinished = this.TestFinished;
                ar.OnTestFailed = this.TestFailed;
                ar.OnTestPassed = this.TestPassed;

                ar.OnErrorMessage = this.OnErrorMessage;
                ar.OnTestOutput = this.TestOutput;
                ar.OnDiagnosticMessage = this.DiagnosticMessage;

                ar.TestCaseFilter = this.TestCaseFilter;
            }

            // Run complete
            void ExecutionComplete( ExecutionCompleteInfo eci )
            {
                if( !verbose )
                    Console.WriteLine();

                int nTotal = eci.TotalTests - eci.TestsSkipped;

                ConsoleColor cc = ( 0 == eci.TestsFailed ) ? ConsoleColor.Green : ConsoleColor.Red;
                ConsoleEx.writeLine( cc, "==== Complete: {0} / {1} OK ====",
                    nTotal - eci.TestsFailed, nTotal );
                completed.Set();
            }

            // Individual tests
            void TestStarting( TestStartingInfo tsi )
            {
                if( !verbose )
                    return;
                ConsoleEx.writeLine( ConsoleColor.DarkGreen, "{0} - starting..", tsi.TestDisplayName );
            }

            void TestFailed( TestFailedInfo tfi )
            {
                if( verbose )
                    ConsoleEx.writeLine( ConsoleColor.Red, "{0} - failed :-(", tfi.TestDisplayName );
                else
                    ConsoleEx.write( ConsoleColor.Red, "F" );

                if( stopOnErrors )
                {
                    ConsoleEx.silent = true;
                    completed.Set();
                    if( Debugger.IsAttached )
                        Debugger.Break();
                }
            }

            void TestPassed( TestPassedInfo tpi )
            {
                if( verbose )
                    ConsoleEx.writeLine( ConsoleColor.Green, "{0} - passed", tpi.TestDisplayName );
                else
                    ConsoleEx.write( ConsoleColor.Green, "." );
            }

            void TestFinished( TestFinishedInfo tfi )
            {
                // ConsoleEx.writeLine( ConsoleColor.Gray, tfi.TestDisplayName );
            }

            // Filter
            bool TestCaseFilter( ITestCase itc )
            {
                if( null == this.strTest )
                    return true;
                return itc.DisplayName.ToLowerInvariant().EndsWith( this.strTest.ToLowerInvariant() );
            }

            // Various messages

            void OnErrorMessage( ErrorMessageInfo emi )
            {
                ConsoleEx.writeLine( ConsoleColor.Yellow, emi.ExceptionMessage );
            }

            void TestOutput( TestOutputInfo toi )
            {
                if( !verbose )
                    return;
                ConsoleEx.writeLine( ConsoleColor.DarkGray, toi.Output );
            }

            void DiagnosticMessage( DiagnosticMessageInfo dmi )
            {
                ConsoleEx.writeLine( ConsoleColor.Blue, dmi.Message );
            }

            public void Run()
            {
                string typeName = null;
                bool diagnosticMessages = true;
                TestMethodDisplay methodDisplay = TestMethodDisplay.Method;
                bool preEnumerateTheories = true;
                bool parallel = false;
                int? maxParallelThreads = null;

                impl.Start( typeName, diagnosticMessages, methodDisplay, preEnumerateTheories, parallel, maxParallelThreads );

                completed.WaitOne();
            }

            public void Dispose()
            {
                impl.Dispose();
            }
        }

        public static void Run( Assembly ass, string single, bool stopOnErrors, bool logToConsole = true )
        {
            Runner r = new Runner( AssemblyRunner.WithoutAppDomain( ass.Location ) );

            if( logToConsole )
                Logger.SetDestination( new MyLogger() );

            r.strTest = single;
            r.stopOnErrors = stopOnErrors;
            r.verbose = logToConsole;

            r.Run();

            r.Dispose();
        }
    }
}