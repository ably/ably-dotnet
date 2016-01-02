using NUnit.Framework;
using System.Diagnostics;
using FluentAssertions;

namespace Ably.AcceptanceTests
{
    [TestFixture]
    class LoggerTests
    {
        class TestLoggerSink : ILoggerSink
        {
            void ILoggerSink.LogEvent( LogLevel level, string message )
            {
                this.lastLevel = level;
                this.lastMessage = message;
            }

            public LogLevel? lastLevel;
            public string lastMessage;
        }

        TestLoggerSink sink = new TestLoggerSink();

        public LoggerTests()
        {
            Logger.logLevel = LogLevel.Debug;
            Logger.SetDestination( sink );
        }


        [Test]
        public void TestLogger()
        {
            sink.lastLevel.ShouldBeEquivalentTo( null );
            sink.lastMessage.ShouldBeEquivalentTo( null );

            Logger.Error( "Test Error Message" );
            sink.lastLevel.ShouldBeEquivalentTo( LogLevel.Error );
            sink.lastMessage.ShouldBeEquivalentTo( "Test Error Message" );

            Logger.Info( "Test Info Message" );
            sink.lastLevel.ShouldBeEquivalentTo( LogLevel.Info );
            sink.lastMessage.ShouldBeEquivalentTo( "Test Info Message" );

            Logger.Debug( "Test Debug Message" );
            sink.lastLevel.ShouldBeEquivalentTo( LogLevel.Debug );
            sink.lastMessage.ShouldBeEquivalentTo( "Test Debug Message" );

            Logger.logLevel = LogLevel.Warning;
            Logger.Error( "Test Error Message" );
            Logger.Info( "Test Info Message" );
            Logger.Debug( "Test Debug Message" );
            sink.lastLevel.ShouldBeEquivalentTo( LogLevel.Error );
            sink.lastMessage.ShouldBeEquivalentTo( "Test Error Message" );
        }
    }
}