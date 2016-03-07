using IO.Ably.Auth;
using IO.Ably.Rest;
using System;
using System.Threading.Tasks;

namespace IO.Ably.ConsoleTest
{
    class Program
    {

        static void Main( string[] args )
        {
            try
            {
                mainImpl().Wait();
                ConsoleColor.Green.writeLine( "Success!" );

            }
            catch( Exception ex )
            {
                ex.logError();
            }
        }

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

        static async Task mainImpl()
        {
            // Create initial REST client
            AblyRest ably = getRestClient();

            // Create a capability with some resources we can use for testing
            Capability capability = new Capability();
            capability.AddResource( "canPublish" ).AllowPublish();
            capability.AddResource( "canAll" ).AllowAll();

            // Authorize
            TokenRequest tokenRequest = createTokenRequest( capability );
            AuthOptions options = new AuthOptions();
            options.AuthUrl = "https://www.ably.io/ably-auth/token-request/demos";

            TokenDetails token = await ably.Auth.RequestToken( tokenRequest, options );

            ConsoleColor.DarkGreen.writeLine( "Authorized OK" );

            // Create another REST client, this time using that key
            ably = new AblyRest( new AblyOptions { Token = token.Token, Environment = AblyEnvironment.Sandbox } );

            ConsoleColor.DarkGreen.writeLine( "Created REST client" );

            // Verify we can publish
            IChannel channel = ably.Channels.Get( "canAll" );
            channel.Publish( "test", true );
        }
    }
}