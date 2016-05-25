using System;
using System.Threading;
using IO.Ably.Rest;
using IO.Ably.Transport;

namespace IO.Ably
{
    public class ClientOptions : AuthOptions
    {
        private string _clientId;

        /// <summary>
        ///
        /// </summary>
        public bool AutoConnect { get; set; }

        /// <summary>
        /// The id of the client represented by this instance. The clientId is relevant
        /// to presence operations, where the clientId is the principal identifier of the
        /// client in presence update messages. The clientId is also relevant to
        /// authentication; a token issued for a specific client may be used to authenticate
        /// the bearer of that token to the service.
        /// </summary>
        public string ClientId
        {
            get { return _clientId; }
            set
            {
                if(value == "*")
                    throw new InvalidOperationException("Wildcard clientIds are not support in ClientOptions");
                _clientId = value;
            }
        }

        public TokenParams DefaultTokenParams { get; set; }


        internal string GetClientId()
        {
            if (ClientId.IsNotEmpty())
                return ClientId;

            return DefaultTokenParams?.ClientId;
        }

        /// <summary>
        /// If <c>false</c>, this disables the default behaviour whereby the library queues messages on a
        /// connection in the disconnected or connecting states. The default behaviour allows applications
        /// to submit messages immediately upon instancing the library, without having to wait for the
        /// connection to be established. Applications may use this option to disable that behaviour if they
        /// wish to have application-level control over the queueing under those conditions.
        /// </summary>
        public bool QueueMessages { get; set; }

        /// <summary>
        /// If <c>false</c>, prevents messages originating from this connection being echoed back on the same
        /// connection.
        /// </summary>
        public bool EchoMessages { get; set; }

        /// <summary>
        /// A connection recovery string, specified by a client when initializing the library
        /// with the intention of inheriting the state of an earlier connection. See the Ably
        /// Realtime API documentation for further information on connection state recovery.
        /// </summary>
        public string Recover { get; set; }

        /// <summary>
        /// For development environments only. Allows a non default host for the realtime service
        /// </summary>
        public string RealtimeHost { get; set; }

        /// <summary>
        /// For development environments only. Allows a non default host for the rest service
        /// </summary>
        public string RestHost { get; set; }

        internal bool IsLiveEnvironment => Environment.HasValue == false || Environment == AblyEnvironment.Live;

        internal bool IsDefaultRestHost => RestHost.IsEmpty() && IsDefaultPort && IsLiveEnvironment;

        internal bool IsDefaultRealtimeHost => RealtimeHost.IsEmpty() && IsDefaultPort && IsLiveEnvironment;

        internal bool IsDefaultPort => Tls ? Port == 80 : TlsPort == 443;

        internal string FullRealtimeHost()
        {
            if (RealtimeHost.IsEmpty())
            {
                if(IsLiveEnvironment)
                    return Defaults.RealtimeHost;
                return Environment.ToString().ToLower() + "-" + Defaults.RealtimeHost;
            }

            return RealtimeHost;
        }

        internal string FullRestHost()
        {
            if (RestHost.IsEmpty())
            {
                if (IsLiveEnvironment)
                {
                    return Defaults.RestHost;
                }
                return Environment.ToString().ToLower() + "-" + Defaults.RestHost;
            }

            return RestHost;
        }

        /// <summary>
        ///  For development environments only; allows a non-default Ably port to be specified.
        /// </summary>
        public int Port { get; set; }
        
        /// <summary>
	    ///Encrypted transport: if true, TLS will be used for all connections (whether REST/HTTP
	    ///or Realtime WebSocket or Comet connections).
	    /// Default: true
        ///</summary>
        public bool Tls { get; set; }

        public int TlsPort { get; set; }

        ///<summary>
        /// If false, forces the library to use the JSON encoding for REST and Realtime operations,
	    /// instead of the default binary msgpack encoding.
	    /// Default: true
        /// </summary>
        public bool UseBinaryProtocol { get; set; }

        public TimeSpan DisconnectedRetryTimeout { get; set; }
        public TimeSpan SuspendedRetryTimeout { get; set; }
        public TimeSpan HttpOpenTimeout { get; set; }
        public TimeSpan HttpRequestTimeout { get; set; }
        public int HttpMaxRetryCount { get; set; }
        public TimeSpan HttpMaxRetryDuration { get; set; }

        /// <summary>
        /// Provides Channels Setting for all Channels created. For more information see <see cref="ChannelOptions"/> 
        /// </summary>
        public ChannelOptions ChannelDefaults { get; set; }

        public AblyEnvironment? Environment { get; set; }

        public ITransportFactory TransportFactory { get; set; }

        public bool CaptureCurrentSynchronizationContext { get; set; } = true;

        public SynchronizationContext CustomContext { get; set; }

        internal AuthMethod Method
        {
            get
            {
                if (ClientId.IsNotEmpty() || AuthUrl != null || AuthCallback != null || Token.IsNotEmpty() ||
                    TokenDetails != null)
                {
                    return Ably.AuthMethod.Token;
                }

                if (Key.IsNotEmpty())
                {
                    return Ably.AuthMethod.Basic;
                }

                //default
                return Ably.AuthMethod.Token;
            }
        }

        internal bool SkipInternetCheck { get; set; } = false;
        internal bool UseSyncForTesting { get; set; } = false;

        internal TimeSpan RealtimeRequestTimeout { get; set; } = Defaults.DefaultRealtimeTimeout;

        /// <summary>
        /// Defaul constructor for ClientOptions
        /// </summary>
        public ClientOptions()
        {
            InitialiseDefaults();
        }

        private void InitialiseDefaults()
        {
            Tls = true;
            TlsPort = 443;
            Port = 80;
            UseBinaryProtocol = true;
            ChannelDefaults = new ChannelOptions(false);
            AutoConnect = true;
            EchoMessages = true;
            QueueMessages = true;
            DisconnectedRetryTimeout = Defaults.DisconnectedRetryTimeout;
            SuspendedRetryTimeout = TimeSpan.FromSeconds(30);
            HttpOpenTimeout = TimeSpan.FromSeconds(4);
            HttpRequestTimeout = TimeSpan.FromSeconds(15);
            HttpMaxRetryCount = 3;
            HttpMaxRetryDuration = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Construct ClientOptions class and set the Key
        /// It automatically parses the key to ensure the correct format is used and sets the KeyId and KeyValue properties
        /// </summary>
        /// <param name="key">Ably authentication key</param>
        public ClientOptions(string key) : base(key)
        {
            InitialiseDefaults();
            ChannelDefaults = new ChannelOptions();
        }
    }
}
