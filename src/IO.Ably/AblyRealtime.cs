using IO.Ably.Realtime;
using IO.Ably.Rest;
using IO.Ably.Transport;
using System;
using System.Net;
using System.Threading.Tasks;

namespace IO.Ably
{
    public class AblyRealtime : IRealtimeClient
    {
        /// <summary></summary>
        /// <param name="key"></param>
        public AblyRealtime(string key)
            : this(new ClientOptions(key))
        { }

        /// <summary>
        ///
        /// </summary>
        /// <param name="options"></param>
        public AblyRealtime(ClientOptions options)
        {
            RestClient = new AblyRest(options);

            ConnectionManager = new ConnectionManager(options, RestClient);
            ChannelFactory = new ChannelFactory() { ConnectionManager = ConnectionManager, Options = options };
            Channels = new ChannelList(ChannelFactory);

            //TODO: Change this and allow a way to check to log exceptions
            if (options.AutoConnect)
                Connect().IgnoreExceptions();
        }

        public ChannelFactory ChannelFactory { get; set; }

        public AblyRest RestClient { get; }

        internal AblyAuth Auth => RestClient.AblyAuth;
        internal ClientOptions Options => RestClient.Options;

        internal ConnectionManager ConnectionManager { get; set; }

        /// <summary>The collection of channels instanced, indexed by channel name.</summary>
        public IRealtimeChannelCommands Channels { get; private set; }

        /// <summary>A reference to the connection object for this library instance.</summary>
        public Connection Connection => ConnectionManager.Connection;

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public async Task<Connection> Connect()
        {
            ConnectionStateType state = Connection.State;
            if (state == ConnectionStateType.Connected)
                return Connection;


            //TODO: To come back to this
            using (ConnStateAwaitor awaitor = new ConnStateAwaitor(Connection))
            {
                awaitor.Connection.Connect();
                return await awaitor.wait();
            }
        }

        /// <summary>
        /// This simply calls connection.close. Causes the connection to close, entering the closed state. Once
        /// closed, the library will not attempt to re-establish the connection without a call to connect().
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
    }
}