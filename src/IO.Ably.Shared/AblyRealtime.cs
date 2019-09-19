using System;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Transport;

namespace IO.Ably
{
    public class AblyRealtime : IRealtimeClient
    {
        internal ILogger Logger { get; private set; }

        internal RealtimeWorkflow Workflow { get; private set; }

        private SynchronizationContext _synchronizationContext;

        public AblyRealtime(string key)
            : this(new ClientOptions(key))
        {
        }

        public AblyRealtime(ClientOptions options)
            : this(options, clientOptions => new AblyRest(clientOptions))
        {
        }

        internal AblyRealtime(ClientOptions options, Func<ClientOptions, AblyRest> createRestFunc)
        {
            Logger = options.Logger;
            CaptureSynchronizationContext(options);
            // Start Reader thread
            RestClient = createRestFunc != null ? createRestFunc.Invoke(options) : new AblyRest(options);
            Channels = new RealtimeChannels(this);
            Connection = new Connection(this, options.NowFunc, options.Logger);

            Connection.Initialise();

            Workflow = new RealtimeWorkflow(Connection, Channels, Logger);
            Workflow.Start();

            RestClient.AblyAuth.AuthUpdated += Connection.ConnectionManager.OnAuthUpdated;

            if (options.AutoConnect)
            {
                Connect();
            }
        }





        private void CaptureSynchronizationContext(ClientOptions options)
        {
            if (options.CustomContext != null)
            {
                _synchronizationContext = options.CustomContext;
            }
            else if (options.CaptureCurrentSynchronizationContext)
            {
                _synchronizationContext = SynchronizationContext.Current;
            }
        }

        public AblyRest RestClient { get; }

        public IAblyAuth Auth => RestClient.AblyAuth;

        public string ClientId => Auth.ClientId;

        internal ClientOptions Options => RestClient.Options;

        internal ConnectionManager ConnectionManager => Connection.ConnectionManager;

        /// <summary>The collection of channels instanced, indexed by channel name.</summary>
        public RealtimeChannels Channels { get; private set; }

        /// <summary>A reference to the connection object for this library instance.</summary>
        public Connection Connection { get; }

        public Task<PaginatedResult<Stats>> StatsAsync()
        {
            return RestClient.StatsAsync();
        }

        public Task<PaginatedResult<Stats>> StatsAsync(StatsRequestParams query)
        {
            return RestClient.StatsAsync(query);
        }

        public PaginatedResult<Stats> Stats()
        {
            return RestClient.Stats();
        }

        public PaginatedResult<Stats> Stats(StatsRequestParams query)
        {
            return RestClient.Stats(query);
        }

        public void Connect()
        {
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
        public Task<DateTimeOffset> TimeAsync()
        {
            return RestClient.TimeAsync();
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
