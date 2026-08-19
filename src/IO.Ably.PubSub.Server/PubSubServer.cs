using System;

namespace IO.Ably.PubSub.Server
{
    /// <summary>
    /// Entry point for Ably Pub/Sub applications that run on a server - an ASP.NET or Azure host, a
    /// worker, a console app or any other backend the end user does not hold.
    /// The returned clients are the ordinary <see cref="AblyRealtime"/> and <see cref="AblyRest"/>, so
    /// the whole of the IO.Ably API remains available; installing this package rather than another
    /// states where the code runs.
    /// </summary>
    public static class PubSubServer
    {
        /// <summary>
        /// Creates a server-side realtime client from an API key.
        /// </summary>
        /// <param name="key"> A valid Ably API key. </param>
        /// <returns> A connected-on-demand realtime client. </returns>
        public static AblyRealtime CreateRealtimeClient(string key)
        {
            return CreateRealtimeClient(new ClientOptions(key));
        }

        /// <summary>
        /// Creates a server-side realtime client from a set of client options.
        /// </summary>
        /// <param name="options"> The client options. </param>
        /// <returns> A connected-on-demand realtime client. </returns>
        public static AblyRealtime CreateRealtimeClient(ClientOptions options)
        {
            return new AblyRealtime(PubSubOptions.Required(options));
        }

        /// <summary>
        /// Creates a server-side realtime client, configured by the supplied action.
        /// </summary>
        /// <param name="init"> Action that populates the client options. </param>
        /// <returns> A connected-on-demand realtime client. </returns>
        public static AblyRealtime CreateRealtimeClient(Action<ClientOptions> init)
        {
            return CreateRealtimeClient(PubSubOptions.Configure(init));
        }

        /// <summary>
        /// Creates a server-side HTTP (REST) client from an API key.
        /// </summary>
        /// <param name="key"> A valid Ably API key. </param>
        /// <returns> An HTTP client. </returns>
        public static AblyRest CreateHttpClient(string key)
        {
            return CreateHttpClient(new ClientOptions(key));
        }

        /// <summary>
        /// Creates a server-side HTTP (REST) client from a set of client options.
        /// </summary>
        /// <param name="options"> The client options. </param>
        /// <returns> An HTTP client. </returns>
        public static AblyRest CreateHttpClient(ClientOptions options)
        {
            return new AblyRest(PubSubOptions.Required(options));
        }

        /// <summary>
        /// Creates a server-side HTTP (REST) client, configured by the supplied action.
        /// </summary>
        /// <param name="init"> Action that populates the client options. </param>
        /// <returns> An HTTP client. </returns>
        public static AblyRest CreateHttpClient(Action<ClientOptions> init)
        {
            return CreateHttpClient(PubSubOptions.Configure(init));
        }
    }
}
