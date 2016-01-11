using Ably.AcceptanceTests;
using Ably.Tests;
using System.Reflection;

namespace Ably.ConsoleTest
{
    static class Program
    {

        static void Main( string[] args )
        {
            // Assembly ass = typeof(LoggerTests).Assembly;

            // Run all of them, with brief output
            // NUnit.Run( ass, false, false, false );

            // Run all of them, with verbose output, and stop on errors
            // NUnit.Run( ass, true, true, true );

            // Run the single test
            // NUnit.Run( ass, true, true, true, "Ably.AcceptanceTests.RealtimeAcceptanceTests(MsgPack).TestCreateRealtimeClient_AutoConnect_False_ConnectsSuccessfuly" );

            Assembly x = typeof( AuthOptionsMergeTests ).Assembly;

            // Run all of them, with verbose output, and stop on errors
            XUnit.Run( x, null, true );

            // Run the single test
            // XUnit.Run( x, "RestTests.Ctor_WithNoParametersAndAblyConnectionString_RetrievesApiKeyFromConnectionString", true );
        }
    }
}