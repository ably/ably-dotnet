using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace IO.Ably.Sandbox
{
    /// <summary>Utility class that helps running tests in the sandbox environment.</summary>
    /// <remarks>Sandbox only holds applications for 1 hour.
    /// Thus, sandboxed apps are only good for some tests of the library.
    /// To test your system you're building on top of this library, we recommend instead requesting your own API key from Ably.</remarks>
    /// <seealso href="https://www.ably.io/" />
    public static class AblySandbox
    {
        const string restHost = "sandbox-rest.ably.io";

        /// <summary>Create a new application on the sandbox.</summary>
        /// <param name="tls">True to use TLS traffic encryption.</param>
        /// <returns>A newly created test application.</returns>
        public static async Task<TestApp> CreateApp( bool tls = true )
        {
            AblyRequest request = new AblyRequest( "/apps", HttpMethod.Post );
            request.Headers.Add( "Accept", "application/json" );
            request.Headers.Add( "Content-Type", "application/json" );
            request.RequestBody = Unzip.resourceBytesGzip( "Sandbox.TestAppSpec.json.gz" );

            AblyHttpClient client = new AblyHttpClient( restHost, null, tls, null );

            AblyResponse response = await client.Execute( request );

            JObject json = JObject.Parse( response.TextResponse );
            return new TestApp( tls, json );
        }
    }
}