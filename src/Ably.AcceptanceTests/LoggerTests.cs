using NUnit.Framework;
using System.Diagnostics;

namespace Ably.AcceptanceTests
{
    [TestFixture]
    class LoggerTests
    {
        public LoggerTests()
        {
            Logger.logLevel = SourceLevels.All;
        }

        [Test]
        public void TestLogger()
        {
            Logger.Error( "Test Error Message" );
            Logger.Info( "Test Info Message" );
            Logger.Debug( "Test Debug Message" );
        }
    }
}