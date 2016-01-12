using Ably.AcceptanceTests;
using Ably.Tests;
using System.Reflection;

namespace Ably.ConsoleTest
{
    static class Program
    {

        static void Main( string[] args )
        {
            // === NUnit ===
            Assembly ass = typeof(LoggerTests).Assembly;

            // Run all of them, with brief output
            // NUnit.Run( ass, false, false, false );

            // Run all of them, with verbose output, and stop on errors
            // NUnit.Run( ass, true, true, true );

            // Run the single test
            // NUnit.Run( ass, true, true, true, "Ably.AcceptanceTests.MessageEncodersAcceptanceTests+WithTextProtocolWithoutEncryption.WithBinaryData_DoesNotApplyAnyEncoding" ); return;

            // === XUnit ===
            Assembly x = typeof( AuthOptionsMergeTests ).Assembly;
            string strTest = null;

            // Run all of them, with brief output
            // XUnit.Run( x, null, false, false );
            // XUnit.Run( x, null, true, true );

            strTest = "DeserializesMessageCorrectly_Messages";
            XUnit.Run( x, strTest, true );
        }
    }
}