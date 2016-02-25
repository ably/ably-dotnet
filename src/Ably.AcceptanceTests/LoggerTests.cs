using NUnit.Framework;
using System.Diagnostics;
using FluentAssertions;

namespace IO.Ably.AcceptanceTests
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

        }


        [Test]
        public void TestLogger()
        {
            using( var d = Logger.SetTempDestination( null ) )
            {
                sink.lastLevel.ShouldBeEquivalentTo( null );
                sink.lastMessage.ShouldBeEquivalentTo( null );

                Logger.logLevel.ShouldBeEquivalentTo( Config.DefaultLogLevel );
                Logger.logLevel = LogLevel.Debug;

                // null destination shouldn't throw
                Logger.SetDestination( null );
                Logger.Info( "msg" );

                Logger.SetDestination( sink );

                // Basic messages
                Logger.Error( "Test Error Message" );
                sink.lastLevel.ShouldBeEquivalentTo( LogLevel.Error );
                sink.lastMessage.ShouldBeEquivalentTo( "Test Error Message" );

                Logger.Info( "Test Info Message" );
                sink.lastLevel.ShouldBeEquivalentTo( LogLevel.Info );
                sink.lastMessage.ShouldBeEquivalentTo( "Test Info Message" );

                Logger.Debug( "Test Debug Message" );
                sink.lastLevel.ShouldBeEquivalentTo( LogLevel.Debug );
                sink.lastMessage.ShouldBeEquivalentTo( "Test Debug Message" );

                // Verify the log level works
                Logger.logLevel = LogLevel.Warning;
                Logger.Error( "Test Error Message" );
                Logger.Info( "Test Info Message" );
                Logger.Debug( "Test Debug Message" );
                sink.lastLevel.ShouldBeEquivalentTo( LogLevel.Error );
                sink.lastMessage.ShouldBeEquivalentTo( "Test Error Message" );

                // Revert the level
                Logger.logLevel = Config.DefaultLogLevel;
            }
        }
    }
}