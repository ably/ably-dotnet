using IO.Ably.Rest;
using System;
using System.Linq;
using System.Threading.Tasks;
using IO.Ably.ConsoleTest.Sandbox;

namespace IO.Ably.ConsoleTest
{
    static class Rest
    {
        static AblyRest GetRestClient()
        {
            ClientOptions options = new ClientOptions();
            options.Environment = AblyEnvironment.Sandbox;
            options.Tls = true;
            options.UseBinaryProtocol = false;
            return new AblyRest( options );
        }

        static TokenParams CreateTokenRequest( Capability capability, TimeSpan? ttl = null )
        {
            var tokenParams = new TokenParams();
            tokenParams.ClientId = "John";
            tokenParams.Capability = capability;
            if( ttl.HasValue )
                tokenParams.Ttl = ttl.Value;
            return tokenParams;
        }

        public static async Task Test()
        {
            ConsoleColor.DarkGreen.writeLine( "Creating sandbox app.." );
            TestApp sandboxTestData = await AblySandbox.CreateApp();

            ConsoleColor.DarkGreen.writeLine( "Creating REST client.." );

            // Create REST client using that key
            AblyRest ably = new AblyRest( sandboxTestData.ToAblyOptions() );

            ConsoleColor.DarkGreen.writeLine( "Publishing a message.." );

            // Verify we can publish
            IChannel channel = ably.Channels.Get( "persisted:presence_fixtures" );

            var tsPublish = DateTimeOffset.UtcNow;
            await channel.Publish( "test", true );

            ConsoleColor.DarkGreen.writeLine( "Getting the history.." );

            PaginatedResource<Message> history = await channel.History();

            if( history.Count <= 0 )
                throw new ApplicationException( "Message lost: not on the history" );

            Message msg = history.First();
            var tsNow = DateTimeOffset.UtcNow;
            var tsHistory = msg.timestamp.Value;

            if( tsHistory < tsPublish )
                throw new ApplicationException( "Timestamp's too early. Please ensure your PC's time is correct, use e.g. time.nist.gov server." );
            if( tsHistory > tsNow )
                throw new ApplicationException( "Timestamp's too late, Please ensure your PC's time is correct, use e.g. time.nist.gov server." );

            ConsoleColor.DarkGreen.writeLine( "Got the history" );
        }
    }
}