using System;

namespace IO.Ably.PubSub.Device
{
    /// <summary>
    /// Entry point for Ably Pub/Sub applications that run on an end-user device - a mobile or desktop
    /// app, a browser, a set-top box or any other client the end user holds.
    /// The returned client is the ordinary <see cref="AblyRealtime"/>, so the whole of the IO.Ably API
    /// remains available; installing this package rather than another states where the code runs.
    /// Device applications that need REST rather than realtime should construct
    /// <see cref="AblyRest"/> directly; there is deliberately no device HTTP door.
    /// </summary>
    public static class PubSubDevice
    {
        /// <summary>
        /// Creates a realtime client for an end-user device from an API key.
        /// </summary>
        /// <param name="key"> A valid Ably API key. </param>
        /// <returns> A connected-on-demand realtime client. </returns>
        public static AblyRealtime CreateClient(string key)
        {
            return CreateClient(new ClientOptions(key));
        }

        /// <summary>
        /// Creates a realtime client for an end-user device from a set of client options.
        /// </summary>
        /// <param name="options"> The client options. </param>
        /// <returns> A connected-on-demand realtime client. </returns>
        public static AblyRealtime CreateClient(ClientOptions options)
        {
            return new AblyRealtime(PubSubOptions.Required(options));
        }

        /// <summary>
        /// Creates a realtime client for an end-user device, configured by the supplied action.
        /// </summary>
        /// <param name="init"> Action that populates the client options. </param>
        /// <returns> A connected-on-demand realtime client. </returns>
        public static AblyRealtime CreateClient(Action<ClientOptions> init)
        {
            return CreateClient(PubSubOptions.Configure(init));
        }
    }
}
