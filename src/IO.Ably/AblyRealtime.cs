using System;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Transport;

namespace IO.Ably
{
    public class AblyRealtime : IRealtimeClient
    {
        private ChannelFactory _channelFactory;

        /// <summary></summary>
        /// <param name="key"></param>
        public AblyRealtime(string key)
            : this(new ClientOptions(key))
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="options"></param>
        public AblyRealtime(ClientOptions options) : 
            this(options, clientOptions => new AblyRest(clientOptions))
        {
            
        }

        internal AblyRealtime(ClientOptions options, Func<ClientOptions, AblyRest> createRestFunc)
        {
            RestClient = createRestFunc(options);
            Connection = new Connection(RestClient);
            Connection.Initialise();
            Channels = new ChannelList(ChannelFactory);

            if (options.AutoConnect)
                Connect();
        }

        public ChannelFactory ChannelFactory => _channelFactory ?? (_channelFactory = new ChannelFactory { ConnectionManager = ConnectionManager, Options = Options });

        public AblyRest RestClient { get; }

        internal AblyAuth Auth => RestClient.AblyAuth;
        internal ClientOptions Options => RestClient.Options;

        internal ConnectionManager ConnectionManager => Connection.ConnectionManager;

        /// <summary>The collection of channels instanced, indexed by channel name.</summary>
        public IRealtimeChannelCommands Channels { get; private set; }

        /// <summary>A reference to the connection object for this library instance.</summary>
        public Connection Connection { get; set; }

        public Task<PaginatedResult<Stats>> Stats()
        {
            return RestClient.Stats();
        }

        public Task<PaginatedResult<Stats>> Stats(StatsDataRequestQuery query)
        {
            return RestClient.Stats(query);
        }

        public Task<PaginatedResult<Stats>> Stats(DataRequestQuery query)
        {
            return RestClient.Stats(query);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public void Connect()
        {
            var state = Connection.State;
            if (state == ConnectionStateType.Connected)
                return;

            Connection.Connect();
        }

        /// <summary>
        ///     This simply calls connection.close. Causes the connection to close, entering the closed state. Once
        ///     closed, the library will not attempt to re-establish the connection without a call to connect().
        /// </summary>
        public void Close()
        {
            Connection.Close();
        }

        /// <summary>Retrieves the ably service time</summary>
        public Task<DateTimeOffset> Time()
        {
            return RestClient.Time();
        }
    }
}