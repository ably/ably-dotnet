using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Xunit;
using Xunit.Runners;

namespace Ably.ConsoleTest
{
    static class XUnit
    {
        class Runner: IDisposable
        {
            readonly AssemblyRunner impl;
            readonly ManualResetEvent completed = new ManualResetEvent( false );

            public bool stopOnErrors = false;

            public Runner( AssemblyRunner ar )
            {
                impl = ar;
                ar.OnExecutionComplete = this.ExecutionComplete;

                ar.OnTestFinished = this.TestFinished;
                ar.OnTestFailed = this.TestFailed;
                ar.OnTestPassed = this.TestPassed;

                ar.OnErrorMessage = this.OnErrorMessage;
                ar.OnTestOutput = this.TestOutput;
                ar.OnDiagnosticMessage = this.DiagnosticMessage;
            }

            // Run complete
            void ExecutionComplete( ExecutionCompleteInfo eci )
            {
                int nTotal = eci.TotalTests - eci.TestsSkipped;

                ConsoleColor cc = ( 0 == eci.TestsFailed ) ? ConsoleColor.Green : ConsoleColor.Red;
                ConsoleEx.writeLine( cc, "OnExecutionComplete: {0} / {1} OK",
                    nTotal - eci.TestsFailed, nTotal );
                completed.Set();
            }

            // Test complete
            void TestFailed( TestFailedInfo tfi )
            {
                ConsoleEx.writeLine( ConsoleColor.Red, "{0} failed", tfi.TestDisplayName );
                if( stopOnErrors )
                {
                    completed.Set();
                    if( Debugger.IsAttached )
                        Debugger.Break();
                }
            }

            void TestPassed( TestPassedInfo tpi )
            {
                ConsoleEx.writeLine( ConsoleColor.Green, "{0} passed", tpi.TestDisplayName );
            }

            void TestFinished( TestFinishedInfo tfi )
            {
                // ConsoleEx.writeLine( ConsoleColor.Gray, tfi.TestDisplayName );
            }

            // Various messages

            void OnErrorMessage( ErrorMessageInfo emi )
            {
                ConsoleEx.writeLine( ConsoleColor.Yellow, emi.ExceptionMessage );
            }


            void TestOutput( TestOutputInfo toi )
            {
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

        public static void Run( Assembly ass, bool stopOnErrors, bool logToConsole = true )
        {
            Runner r = new Runner(AssemblyRunner.WithoutAppDomain( ass.Location ));

            if( logToConsole )
                Logger.SetDestination( new MyLogger() );
            r.stopOnErrors = stopOnErrors;

            r.Run();

            r.Dispose();
        }
    }
}