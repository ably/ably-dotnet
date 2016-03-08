using IO.Ably.Rest;
using IO.Ably.Sandbox;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IO.Ably.ConsoleTest
{
    static class Rest
    {
        static AblyRest getRestClient()
        {
            AblyOptions options = new AblyOptions();
            options.Environment = AblyEnvironment.Sandbox;
            options.Tls = true;
            options.UseBinaryProtocol = false;
            return new AblyRest( options );
        }

        static TokenRequest createTokenRequest( Capability capability, TimeSpan? ttl = null )
        {
            TokenRequest res = new TokenRequest();
            res.ClientId = "John";
            res.Capability = capability;
            if( ttl.HasValue )
                res.Ttl = ttl.Value;
            return res;
        }

        public static async Task test()
        {
            ConsoleColor.DarkGreen.writeLine( "Creating sandbox app.." );
            TestApp sandboxTestData = await AblySandbox.CreateApp();

            ConsoleColor.DarkGreen.writeLine( "Creating REST client.." );

            // Create REST client using that key
            AblyRest ably = new AblyRest( sandboxTestData.ToAblyOptions() );

            ConsoleColor.DarkGreen.writeLine( "Publishing a message.." );

            // Verify we can publish
            IChannel channel = ably.Channels.Get( "persisted:presence_fixtures" );

            DateTime tsPublish = DateTime.UtcNow;
            await channel.Publish( "test", true );

            ConsoleColor.DarkGreen.writeLine( "Getting the history.." );

            PaginatedResource<Message> history = await channel.History();

            if( history.Count <= 0 )
                throw new ApplicationException( "Message lost: not on the history" );

            Message msg = history.First();
            DateTime tsNow = DateTime.UtcNow;
            DateTime tsHistory = msg.timestamp.Value;

            if( tsHistory < tsPublish )
                throw new ApplicationException( "Timestamp's too early. Please ensure your PC's time is correct, use e.g. time.nist.gov server." );
            if( tsHistory > tsNow )
                throw new ApplicationException( "Timestamp's too late, Please ensure your PC's time is correct, use e.g. time.nist.gov server." );

            ConsoleColor.DarkGreen.writeLine( "Got the history" );
        }
    }
}