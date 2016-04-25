using IO.Ably.Realtime;
using IO.Ably.Rest;
using IO.Ably.Transport;
using System;
using System.Net;
using System.Threading.Tasks;

namespace IO.Ably
{
    public class AblyRealtime
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

            //TODO: Change this and allow a way to check to log exceptions
            if (options.AutoConnect)
                Connect().IgnoreExceptions();
        }

        public AblyRest RestClient { get; }

        internal AblyAuth Auth => RestClient.AblyAuth;
        internal ClientOptions Options => RestClient.Options;

        /// <summary>The collection of channels instanced, indexed by channel name.</summary>
        public IRealtimeChannelCommands Channels { get; private set; }

        /// <summary>A reference to the connection object for this library instance.</summary>
        public Connection Connection { get; private set; }

        ClientOptions options => Options;

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public async Task<Connection> Connect()
        {
            if (Channels == null || Connection == null)
            {
                await InitAuth();

                IConnectionManager connectionManager = new ConnectionManager(options);
                IChannelFactory factory = new ChannelFactory() { ConnectionManager = connectionManager, Options = options };
                Channels = new ChannelList(connectionManager, factory);
                Connection = connectionManager.Connection;
            }

            ConnectionState state = this.Connection.State;
            if (state == ConnectionState.Connected)
                return Connection;

            using (ConnStateAwaitor awaitor = new ConnStateAwaitor(this.Connection))
            {
                awaitor.connection.Connect();
                return await awaitor.wait();
            }
        }

        internal async Task InitAuth()
        {
            if (Auth.AuthMethod == AuthMethod.Basic)
            {
                string authHeader = Convert.ToBase64String(Options.Key.GetBytes());
                //TODO: Fix this.
                options.AuthHeaders["Authorization"] = "Basic " + authHeader;
                return;
            }
            if (Auth.AuthMethod == AuthMethod.Token)
            {
                await InitTokenAuth();
                return;
            }

            throw new Exception("Unexpected AuthMethod value");
        }

        /// <summary>Request auth token, set options</summary>
        async Task InitTokenAuth()
        {
            await Auth.Authorise(); //This sets current token

            if (Auth.HasValidToken())
            {
                // WebSockets use Token from Options.Token
                Options.Token = Auth.CurrentToken.Token;
                Logger.Debug("Adding Authorization header with Token authentication");
            }
            else
                throw new AblyException("Invalid token credentials: " + Auth.CurrentToken, 40100, HttpStatusCode.Unauthorized);
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
    }
}