using System;
using System.Threading;
using IO.Ably.Transport;
using Newtonsoft.Json;

namespace IO.Ably
{
    /// <summary>
    /// Options: Ably library options for REST and Realtime APIs.
    /// </summary>
    public class ClientOptions : AuthOptions
    {
        private string _clientId;
        private Func<DateTimeOffset> _nowFunc;
        private bool _useBinaryProtocol = false;
        private string[] _fallbackHosts;

        /// <summary>
        /// If false, suppresses the automatic initiation of a connection when the library is instanced.
        /// Default: true.
        /// </summary>
        public bool AutoConnect { get; set; } = true;

        /// <summary>
        /// The id of the client represented by this instance. The clientId is relevant
        /// to presence operations, where the clientId is the principal identifier of the
        /// client in presence update messages. The clientId is also relevant to
        /// authentication; a token issued for a specific client may be used to authenticate
        /// the bearer of that token to the service.
        /// Default: null.
        /// </summary>
        public string ClientId
        {
            get => _clientId;

            set
            {
                if (value == "*")
                {
                    throw new InvalidOperationException("Wildcard clientIds are not support in ClientOptions");
                }

                _clientId = value;
            }
        }

        /// <summary>
        /// When a TokenParams object is provided, it will override
        /// the client library defaults described in TokenParams
        /// Spec: TO3j11.
        /// Default: null.
        /// </summary>
        public TokenParams DefaultTokenParams { get; set; }

        internal string GetClientId()
        {
            if (ClientId.IsNotEmpty())
            {
                return ClientId;
            }

            return DefaultTokenParams?.ClientId;
        }

        /// <summary>
        /// If <c>false</c>, this disables the default behaviour whereby the library queues messages on a
        /// connection in the disconnected or connecting states. The default behaviour allows applications
        /// to submit messages immediately upon instancing the library, without having to wait for the
        /// connection to be established. Applications may use this option to disable that behaviour if they
        /// wish to have application-level control over the queueing under those conditions.
        /// Default: true.
        /// </summary>
        public bool QueueMessages { get; set; } = true;

        /// <summary>
        /// If <c>false</c>, prevents messages originating from this connection being echoed back on the same
        /// connection.
        /// Default: true.
        /// </summary>
        public bool EchoMessages { get; set; } = true;

        /// <summary>
        /// A connection recovery string, specified by a client when initializing the library
        /// with the intention of inheriting the state of an earlier connection. See the Ably
        /// Realtime API documentation for further information on connection state recovery.
        /// Default: null.
        /// </summary>
        public string Recover { get; set; }

        /// <summary>
        /// For development environments only. Allows a non default host for the realtime service.
        /// Default: null.
        /// </summary>
        public string RealtimeHost { get; set; }

        /// <summary>
        /// Log level; controls the level of verbosity of log messages from the library.
        /// Default: null. Which means the log level is set to Warning.
        /// </summary>
        public LogLevel? LogLevel { get; set; }

        /// <summary>
        /// Log handler: allows the client to intercept log messages and handle them in a client-specific way.
        /// Default: null.
        /// </summary>
        [JsonIgnore]
        public ILoggerSink LogHander { get; set; }

        /// <summary>
        /// For development environments only. Allows a non default host for the rest service.
        /// Default: null.
        /// </summary>
        public string RestHost { get; set; }

        /// <summary>
        /// Gets or sets an array of custom Fallback hosts to be (optionally) used in place of the defaults.
        /// If an empty array is specified, then fallback host functionality is disabled.
        /// </summary>
        public string[] FallbackHosts
        {
            get
            {
                if (_fallbackHosts is null)
                {
                    return Defaults.FallbackHosts;
                }

                return _fallbackHosts;
            }

            set => _fallbackHosts = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use default FallbackHosts even when overriding
        /// environment or restHost/realtimeHost.
        /// </summary>
        public bool FallbackHostsUseDefault { get; set; }

        internal bool IsLiveEnvironment => Environment.IsEmpty() || Environment == "live";

        internal bool IsDefaultRestHost => RestHost.IsEmpty() && IsDefaultPort && IsLiveEnvironment;

        internal bool IsDefaultRealtimeHost => RealtimeHost.IsEmpty() && IsDefaultPort && IsLiveEnvironment;

        internal bool IsDefaultPort => Tls ? Port == 80 : TlsPort == 443;

        internal string FullRealtimeHost()
        {
            if (RealtimeHost.IsEmpty())
            {
                if (IsLiveEnvironment)
                {
                    return Defaults.RealtimeHost;
                }

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
        /// For development environments only; allows a non-default Ably port to be specified.
        /// Default: 80.
        /// </summary>
        public int Port { get; set; } = 80;

        /// <summary>
        /// Encrypted transport: if true, TLS will be used for all connections (whether REST/HTTP
        /// or Realtime WebSocket or Comet connections).
        /// Default: true.
        /// </summary>
        public bool Tls { get; set; } = true;

        /// <summary>
        /// Allows non-default Tls port to be specified.
        /// Default: 443.
        /// </summary>
        public int TlsPort { get; set; } = 443;

        /// <summary>
        /// If false, forces the library to use the JSON encoding for REST and Realtime operations,
        /// If true, the MsgPack binary format is used (if available in the current build
        /// Default: false.
        /// </summary>
        public bool UseBinaryProtocol
        {
#if MSGPACK
            get { return _useBinaryProtocol; }
#else
            get { return false; }
#endif
            set { _useBinaryProtocol = value; }
        }

        /// <summary>
        /// Number of seconds before the library will retry after a disconnect.
        /// Default: 15s.
        /// </summary>
        public TimeSpan DisconnectedRetryTimeout { get; set; } = Defaults.DisconnectedRetryTimeout;

        /// <summary>
        /// Number of seconds before the library will retry after reaching Suspended state.
        /// Default: 30s.
        /// </summary>
        public TimeSpan SuspendedRetryTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Number of seconds before a channel will try to reattach itself after becoming suspended.
        /// Default: 15s.
        /// </summary>
        public TimeSpan ChannelRetryTimeout { get; set; } = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Timeout for opening an http request.
        /// Default: 4s.
        /// </summary>
        public TimeSpan HttpOpenTimeout { get; set; } = TimeSpan.FromSeconds(4);

        /// <summary>
        /// Timeout for completing an http request.
        /// Default: 10s.
        /// </summary>
        public TimeSpan HttpRequestTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Maximum number of Http retries the library will attempt.
        /// Default: 3.
        /// </summary>
        public int HttpMaxRetryCount { get; set; } = 3;

        /// <summary>
        /// Duration for which the library will retry an http request.
        /// Default: 15s.
        /// </summary>
        public TimeSpan HttpMaxRetryDuration { get; set; } = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Provides Channels Setting for all Channels created. For more information see <see cref="ChannelOptions"/>.
        /// </summary>
        public ChannelOptions ChannelDefaults { get; internal set; } = new ChannelOptions();

        /// <summary>
        /// For development; allows to specify a non production environment.
        /// Default: null.
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// Allows the user to override the Websocket Transport Factory and provide their own implementation.
        /// </summary>
        [JsonIgnore]
        public ITransportFactory TransportFactory { get; set; }

        /// <summary>
        /// Switched on IdempotentRest publishing. Currently switched off by default in libraries with version less than 1.2.
        /// </summary>
        public bool IdempotentRestPublishing { get; set; }

        /// <summary>
        /// [Obsolete] Tells the library whether to capture the current SynchronizationContext and use it when triggering handlers and emitters
        /// It will be removed in the next version of the library.
        /// Default: true.
        /// </summary>
        [Obsolete("We will no longer support the SynchronizationContext in the library. This property will be removed in future versions")]
        public bool CaptureCurrentSynchronizationContext { get; set; } = true;

        /// <summary>
        /// [Obsolete] Allows developers to provide their Captured Context to be used when triggering handlers and emitters
        /// It will be removed in the next version of the library.
        /// Default: null.
        /// </summary>
        [Obsolete("We will no longer support the SynchronizationContext in the library. This property will be removed in future versions")]
        public SynchronizationContext CustomContext { get; set; }

        [JsonIgnore]
        internal Func<DateTimeOffset> NowFunc
        {
            get => _nowFunc ?? (_nowFunc = Defaults.NowFunc());
            set => _nowFunc = value;
        }

        [JsonIgnore]
        internal ILogger Logger { get; set; } = DefaultLogger.LoggerInstance;

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

                // default
                return Ably.AuthMethod.Token;
            }
        }

        internal bool SkipInternetCheck { get; set; } = false;

        internal TimeSpan RealtimeRequestTimeout { get; set; } = Defaults.DefaultRealtimeTimeout;

        /// <summary>
        /// Default constructor for ClientOptions.
        /// </summary>
        public ClientOptions()
        {
            Init();
        }

        /// <summary>
        /// Construct ClientOptions class and set the Key
        /// It automatically parses the key to ensure the correct format is used and sets the KeyId and KeyValue properties.
        /// </summary>
        /// <param name="key">Ably authentication key.</param>
        public ClientOptions(string key)
            : base(key)
        {
            Init();
        }

        private void Init()
        {
            SetIdempotentRestPublishingDefault(Defaults.ProtocolMajorVersion, Defaults.ProtocolMinorVersion);
        }

        internal void SetIdempotentRestPublishingDefault(int majorVersion, int minorVersion)
        {
            // (TO3n) idempotentRestPublishing defaults to false for clients with version < 1.1, otherwise true.
            if ((majorVersion == 1 && minorVersion >= 2) || majorVersion > 1)
            {
                IdempotentRestPublishing = true;
            }
            else
            {
                IdempotentRestPublishing = false;
            }
        }
    }
}
