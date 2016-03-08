using IO.Ably.Auth;
using IO.Ably.Rest;
using IO.Ably.Sandbox;
using System;
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
            TestApp sandboxTestData = await AblySandbox.CreateApp();

            ConsoleColor.DarkGreen.writeLine( "Created sandbox app" );

            // Create REST client using that key
            AblyRest ably = new AblyRest( sandboxTestData.ToAblyOptions() );

            ConsoleColor.DarkGreen.writeLine( "Created REST client" );

            // Verify we can publish
            IChannel channel = ably.Channels.Get( "persisted:presence_fixtures" );
            await channel.Publish( "test", true );

            ConsoleColor.DarkGreen.writeLine( "Publish succeeded" );
        }
    }
}