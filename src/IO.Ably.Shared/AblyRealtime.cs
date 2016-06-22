using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Rest;
using IO.Ably.Transport;

namespace IO.Ably
{
    public class AblyRealtime : IRealtimeClient, IRealtimeChannelCommands
    {
        private SynchronizationContext _synchronizationContext;

        internal ConcurrentDictionary<string, IRealtimeChannel> RealtimeChannels { get; private set; } = new ConcurrentDictionary<string, IRealtimeChannel>();

        public AblyRealtime(string key)
            : this(new ClientOptions(key))
        {
        }

        public AblyRealtime(ClientOptions options) :
            this(options, clientOptions => new AblyRest(clientOptions))
        {

        }

        internal AblyRealtime(ClientOptions options, Func<ClientOptions, AblyRest> createRestFunc)
        {
            CaptureSynchronizationContext(options);

            RestClient = createRestFunc(options);
            Connection = new Connection(this);
            Connection.Initialise();

            if (options.AutoConnect)
                Connect();
        }

        private void CaptureSynchronizationContext(ClientOptions options)
        {
            if (options.CustomContext != null)
                _synchronizationContext = options.CustomContext;
            else if (options.CaptureCurrentSynchronizationContext)
            {
                _synchronizationContext = SynchronizationContext.Current;
            }
        }

        public AblyRest RestClient { get; }

        public IAblyAuth Auth => RestClient.AblyAuth;
        internal ClientOptions Options => RestClient.Options;

        internal ConnectionManager ConnectionManager => Connection.ConnectionManager;

        /// <summary>The collection of channels instanced, indexed by channel name.</summary>
        public IRealtimeChannelCommands Channels => this;

        /// <summary>A reference to the connection object for this library instance.</summary>
        public Connection Connection { get; set; }

        public Task<PaginatedResult<Stats>> StatsAsync()
        {
            return RestClient.StatsAsync();
        }

        public Task<PaginatedResult<Stats>> StatsAsync(StatsDataRequestQuery query)
        {
            return RestClient.StatsAsync(query);
        }

        public Task<PaginatedResult<Stats>> StatsAsync(DataRequestQuery query)
        {
            return RestClient.StatsAsync(query);
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
            return RestClient.TimeAsync();
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
                        channel.StateChanged -= eventHandler;
                        IRealtimeChannel removedChannel;
                        if (RealtimeChannels.TryRemove(name, out removedChannel))
                            (removedChannel as RealtimeChannel).Dispose();
                    }
                };
                channel.StateChanged += eventHandler;
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

        internal void NotifyExternalClients(Action action)
        {
            var context = Volatile.Read(ref _synchronizationContext);
            if (context != null)
            {
                context.Post(delegate { action(); }, null);
            }
            else
            {
                action();
            }
        }
    }
}