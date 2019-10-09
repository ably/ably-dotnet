using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Transport;
using Newtonsoft.Json.Linq;

namespace IO.Ably
{
    /// <summary>
    /// AblyRealtime
    /// The top-level class for the Ably Realtime library.
    /// </summary>
    public class AblyRealtime : IRealtimeClient, IDisposable
    {
        internal ILogger Logger { get; private set; }

        internal RealtimeWorkflow Workflow { get; private set; }

        private SynchronizationContext _synchronizationContext;

        internal volatile bool Disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="AblyRealtime"/> class with an ably key.
        /// </summary>
        /// <param name="key">String key (obtained from application dashboard).</param>
        public AblyRealtime(string key)
            : this(new ClientOptions(key))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AblyRealtime"/> class with the given options.
        /// </summary>
        /// <param name="options"><see cref="ClientOptions"/>.</param>
        public AblyRealtime(ClientOptions options)
            : this(options, clientOptions => new AblyRest(clientOptions))
        {
        }

        internal AblyRealtime(ClientOptions options, Func<ClientOptions, AblyRest> createRestFunc)
        {
            Logger = options.Logger;
            RestClient = createRestFunc != null ? createRestFunc.Invoke(options) : new AblyRest(options);

            Connection = new Connection(this, options.NowFunc, options.Logger);
            Connection.Initialise();

            Channels = new RealtimeChannels(this, Connection);
            RestClient.AblyAuth.OnAuthUpdated = ConnectionManager.OnAuthUpdated;

            State = new RealtimeState(options.FallbackHosts?.Shuffle().ToList());

            Workflow = new RealtimeWorkflow(this, Logger);
            Workflow.Start();

            if (options.AutoConnect)
            {
                Connect();
            }
        }

        /// <summary>
        /// Gets the initialised RestClient.
        /// </summary>
        public AblyRest RestClient { get; }

        /// <summary>
        /// Gets the initialised Auth.
        /// </summary>
        public IAblyAuth Auth => RestClient.AblyAuth;

        /// <summary>
        /// Current client id.
        /// </summary>
        public string ClientId => Auth.ClientId;

        internal ClientOptions Options => RestClient.Options;

        internal ConnectionManager ConnectionManager => Connection.ConnectionManager;

        /// <summary>The collection of channels instanced, indexed by channel name.</summary>
        public RealtimeChannels Channels { get; private set; }

        /// <summary>A reference to the connection object for this library instance.</summary>
        public Connection Connection { get; }

        internal RealtimeState State { get; }

        /// <summary>
        /// Convenience method to get Stats with default parameters.
        /// </summary>
        /// <returns>returns a PaginatedResult of Stats.</returns>
        public Task<PaginatedResult<Stats>> StatsAsync()
        {
            return RestClient.StatsAsync();
        }

        /// <summary>
        /// Convenience method to get Stats by passing <see cref="StatsRequestParams"/>.
        /// </summary>
        /// <param name="query"><see cref="StatsRequestParams"/>.</param>
        /// <returns>returns a PaginatedResult of Stats.</returns>
        public Task<PaginatedResult<Stats>> StatsAsync(StatsRequestParams query)
        {
            return RestClient.StatsAsync(query);
        }

        /// <summary>
        /// Sync version of <see cref="StatsAsync()"/>.
        /// </summary>
        /// <returns>returns a PaginatedResult of Stats.</returns>
        public PaginatedResult<Stats> Stats()
        {
            return RestClient.Stats();
        }

        /// <summary>
        /// Sync version of <see cref="StatsAsync(StatsRequestParams)"/>.
        /// </summary>
        /// <param name="query"><see cref="StatsRequestParams"/>.</param>
        /// <returns>returns a PaginatedResult of Stats.</returns>
        public PaginatedResult<Stats> Stats(StatsRequestParams query)
        {
            return RestClient.Stats(query);
        }

        /// <summary>
        /// Initiate a connection.
        /// </summary>
        public void Connect()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException("This instance has been disposed. Please create a new one.");
            }

            Connection.Connect();
        }

        /// <summary>
        ///     This simply calls connection.close. Causes the connection to close, entering the closed state. Once
        ///     closed, the library will not attempt to re-establish the connection without a call to connect().
        /// </summary>
        public void Close()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException("This instance has been disposed. Please create a new one.");
            }

            Connection.Close();
        }

        /// <summary>
        /// Retrieves the ably service time.
        /// </summary>
        /// <returns>returns current server time as DateTimeOffset.</returns>
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

        /// <summary>
        /// Debug method to get the full library state.
        /// Useful when trying to figure out the full state of the library.
        /// </summary>
        /// <returns>json object of the full state of the library.</returns>
        public string GetCurrentState()
        {
            var result = new JObject();
            result["options"] = JObject.FromObject(Options);
            result["state"] = State.WhatDoIHave();
            result["channels"] = Channels.GetCurrentState();
            result["isDisposed"] = Disposed;
            return result.ToString();
        }

        /// <summary>
        /// Disposes the current instance.
        /// Once disposed, it closes the connection and the library can't be used again.
        /// </summary>
        public void Dispose()
        {
            // TODO : Need to move to disposing state and then disposed.
            try
            {
                Connection?.Dispose();
                Workflow.Close();
            }
            catch (Exception e)
            {
                Logger.Error("Error disposing Ably Realtime", e);
            }

            Disposed = true;
        }
    }
}
