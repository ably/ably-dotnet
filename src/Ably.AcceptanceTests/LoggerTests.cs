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
            Logger.Current.Error( "Test Error Message" );
            Logger.Current.Info( "Test Info Message" );
            Logger.Current.Debug( "Test Debug Message" );
        }
    }
}