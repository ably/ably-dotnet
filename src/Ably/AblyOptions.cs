using Ably.Rest;

namespace Ably
{
    /// <summary>
    /// Ably options class. It is used to instantiate Ably.Rest.Client
    /// </summary>
    public class AblyOptions : AuthOptions
    {
        /// <summary>
        /// The id of the client represented by this instance. The clientId is relevant
	    /// to presence operations, where the clientId is the principal identifier of the
	    /// client in presence update messages. The clientId is also relevant to
	    /// authentication; a token issued for a specific client may be used to authenticate
	    /// the bearer of that token to the service.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// For development environments only. Allows a non default host to be specified
        /// </summary>
        public string Host { get; set; }
        
        /// <summary>
        ///  For development environments only; allows a non-default Ably port to be specified.
        /// </summary>
        public int? Port { get; set; }
        
        /// <summary>
	    ///Encrypted transport: if true, TLS will be used for all connections (whether REST/HTTP
	    ///or Realtime WebSocket or Comet connections).
	    /// Default: true
        ///</summary>
        public bool Tls { get; set; }

        ///<summary>
        /// If false, forces the library to use the JSON encoding for REST and Realtime operations,
	    /// instead of the default binary msgpack encoding.
	    /// Default: true
        /// </summary>
        public bool UseBinaryProtocol { get; set; }

        /// <summary>
        /// Provides Channels Setting for all Channels created. For more information see <see cref="ChannelOptions"/> 
        /// </summary>
        public ChannelOptions ChannelDefaults { get; set; }

        internal AblyEnvironment? Environment { get; set; }

        internal AuthMethod Method
        {
            get
            {
                if (Key.IsNotEmpty())
                {
                    if (ClientId.IsEmpty())
                    {
                        return Ably.AuthMethod.Basic;
                    }
                }

                return Ably.AuthMethod.Token;
            }
        }

        /// <summary>
        /// Defaul constructor for AblyOptions
        /// </summary>
        public AblyOptions()
        {
            Tls = true;
            UseBinaryProtocol = true;
            ChannelDefaults = new ChannelOptions();
        }

        /// <summary>
        /// Construct AblyOptions class and set the Key
        /// It automatically parses the key to ensure the correct format is used and sets the KeyId and KeyValue properties
        /// </summary>
        /// <param name="key">Ably authentication key</param>
        public AblyOptions(string key) : base(key)
        {
            Tls = true;
            UseBinaryProtocol = true;
            ChannelDefaults = new ChannelOptions();
        }
    }
}
