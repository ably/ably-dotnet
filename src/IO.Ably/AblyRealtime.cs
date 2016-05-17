using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Rest;
using IO.Ably.Transport;

namespace IO.Ably
{
    public class AblyRealtime : IRealtimeClient, IRealtimeChannelCommands
    {
        private readonly object _channelLock = new object();
        internal Dictionary<string, IRealtimeChannel> RealtimeChannels { get; private set; } = new Dictionary<string, IRealtimeChannel>();
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

            if (options.AutoConnect)
                Connect();
        }

        public AblyRest RestClient { get; }

        internal AblyAuth Auth => RestClient.AblyAuth;
        internal ClientOptions Options => RestClient.Options;

        internal ConnectionManager ConnectionManager => Connection.ConnectionManager;

        /// <summary>The collection of channels instanced, indexed by channel name.</summary>
        public IRealtimeChannelCommands Channels => this;

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

        public IRealtimeChannel Get(string name)
        {
            IRealtimeChannel channel = null;
            if (!RealtimeChannels.TryGetValue(name, out channel))
            {
                channel = new RealtimeChannel(name, Options.GetClientId(), ConnectionManager);
                RealtimeChannels.Add(name, channel);
            }
            return channel;
        }

        public IRealtimeChannel Get(string name, ChannelOptions options)
        {
            var channel = Get(name);
            channel.Options = options;
            return channel;
        }

        public IRealtimeChannel this[string name] => Get(name);

        public void Release(string name)
        {
            IRealtimeChannel channel = null;
            if (RealtimeChannels.TryGetValue(name, out channel))
            {
                EventHandler<ChannelStateChangedEventArgs> eventHandler = null;
                eventHandler = (s, args) =>
                {
                    if (args.NewState == ChannelState.Detached || args.NewState == ChannelState.Failed)
                    {
                        channel.ChannelStateChanged -= eventHandler;
                        RealtimeChannels.Remove(name);
                    }
                };
                channel.ChannelStateChanged += eventHandler;
                channel.Detach();
            }
        }

        public void ReleaseAll()
        {
            var channelList = RealtimeChannels.Keys.ToArray();
            foreach (var channelName in channelList)
            {
                Release(channelName);
            }
        }

        public IEnumerator<IRealtimeChannel> GetEnumerator()
        {
            return RealtimeChannels.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}