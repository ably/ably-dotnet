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
        private SynchronizationContext _synchronizationContext;

        internal ILogger Logger { get; private set; }

        internal RealtimeWorkflow Workflow { get; private set; }

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
            CaptureSynchronizationContext(options);
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

        /// <summary>
        /// Gets the initialised RestClient.
        /// </summary>
        public AblyRest RestClient { get; }

        /// <inheritdoc/>
        public IAblyAuth Auth => RestClient.AblyAuth;

        /// <inheritdoc/>
        public string ClientId => Auth.ClientId;

        internal ClientOptions Options => RestClient.Options;

        internal ConnectionManager ConnectionManager => Connection.ConnectionManager;

        /// <inheritdoc/>
        public RealtimeChannels Channels { get; private set; }

        /// <inheritdoc/>
        public Connection Connection { get; }

        internal RealtimeState State { get; }

        /// <inheritdoc/>
        public Task<PaginatedResult<Stats>> StatsAsync()
        {
            return RestClient.StatsAsync();
        }

        /// <inheritdoc/>
        public Task<PaginatedResult<Stats>> StatsAsync(StatsRequestParams query)
        {
            return RestClient.StatsAsync(query);
        }

        /// <inheritdoc/>
        public PaginatedResult<Stats> Stats()
        {
            return RestClient.Stats();
        }

        /// <inheritdoc/>
        public PaginatedResult<Stats> Stats(StatsRequestParams query)
        {
            return RestClient.Stats(query);
        }

        /// <inheritdoc/>
        public void Connect()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException("This instance has been disposed. Please create a new one.");
            }

            Connection.Connect();
        }

        /// <inheritdoc/>
        public void Close()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException("This instance has been disposed. Please create a new one.");
            }

            Connection.Close();
        }

        /// <inheritdoc/>
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
            Dispose(true);
        }

        /// <summary>
        /// Disposes the current instance.
        /// Once disposed, it closes the connection and the library can't be used again.
        /// </summary>
        /// <param name="disposing">Whether the dispose method triggered it directly.</param>
        protected void Dispose(bool disposing)
        {
            if (Disposed)
            {
                return;
            }

            if (disposing)
            {
                try
                {
                    Connection?.RemoveAllListeners();
                    Channels?.CleanupChannels();
                }
                catch (Exception e)
                {
                    Logger.Error("Error disposing Ably Realtime", e);
                }
            }

            Workflow.QueueCommand(DisposeCommand.Create().TriggeredBy($"AblyRealtime.Dispose({disposing}"));

            Disposed = true;
        }
    }
}
