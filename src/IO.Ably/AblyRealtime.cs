using System;
using System.Collections;
using System.Collections.Concurrent;
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
        internal ConcurrentDictionary<string, IRealtimeChannel> RealtimeChannels { get; private set; } = new ConcurrentDictionary<string, IRealtimeChannel>();

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
            Connection = new Connection(this);
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

        public IRealtimeChannel Get(string name, ChannelOptions options = null)
        {
            IRealtimeChannel result = null;
            if (!RealtimeChannels.TryGetValue(name, out result))
            {
                var channel = new RealtimeChannel(name, Options.GetClientId(), this, options);
                result = RealtimeChannels.AddOrUpdate(name, channel, (s, realtimeChannel) =>
                {
                    if (options != null)
                    {
                        realtimeChannel.Options = options;
                    }
                    return realtimeChannel;
                });
            }
            else
            {
                if (options != null)
                    result.Options = options;
            }
            return result;
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
                        IRealtimeChannel removedChannel;
                        RealtimeChannels.TryRemove(name, out removedChannel);
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

        public bool ContainsChannel(string name)
        {
            return RealtimeChannels.ContainsKey(name);
        }

        public IEnumerator<IRealtimeChannel> GetEnumerator()
        {
            return RealtimeChannels.ToArray().Select(x => x.Value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}