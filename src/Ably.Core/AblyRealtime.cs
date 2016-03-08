using IO.Ably.Realtime;
using IO.Ably.Rest;
using IO.Ably.Transport;
using System;
using System.Net;
using System.Threading.Tasks;

namespace IO.Ably
{
    /// <summary>
    ///
    /// </summary>
    public class AblyRealtime : AblyBase
    {
        /// <summary></summary>
        /// <param name="key"></param>
        public AblyRealtime( string key )
            : this( new AblyRealtimeOptions( key ) )
        { }

        /// <summary>
        ///
        /// </summary>
        /// <param name="options"></param>
        public AblyRealtime( AblyRealtimeOptions options )
        {
            _options = options;
            if( options.AutoConnect )
                this.Connect().IgnoreExceptions();
        }

        Rest.AblySimpleRestClient _simpleRest;

        /// <summary>The collection of channels instanced, indexed by channel name.</summary>
        public IRealtimeChannelCommands Channels { get; private set; }

        /// <summary>A reference to the connection object for this library instance.</summary>
        public Connection Connection { get; private set; }

        AblyRealtimeOptions options { get { return _options as AblyRealtimeOptions; } }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public async Task<Connection> Connect()
        {
            if( null == this.Channels || null == this.Connection )
            {
                _simpleRest = new Rest.AblySimpleRestClient( _options );
                await InitAuth( _simpleRest );

                IConnectionManager connectionManager = new ConnectionManager( options );
                _protocol = _options.UseBinaryProtocol == false ? Protocol.Json : Protocol.MsgPack;
                IChannelFactory factory = new ChannelFactory() { ConnectionManager = connectionManager, Options = options };
                this.Channels = new ChannelList( connectionManager, factory );
                this.Connection = connectionManager.Connection;
            }

            ConnectionState state = this.Connection.State;
            if( state == ConnectionState.Connected )
                return this.Connection;

            using( ConnStateAwaitor awaitor = new ConnStateAwaitor( this.Connection ) )
            {
                awaitor.connection.Connect();
                return await awaitor.wait();
            }
        }

        new internal async Task InitAuth( IAblyRest restClient )
        {
            base.InitAuth( restClient );

            if( AuthMethod == AuthMethod.Basic )
            {
                string authHeader = Convert.ToBase64String(Options.Key.GetBytes());
                options.AuthHeaders[ "Authorization" ] = "Basic " + authHeader;
                return;
            }
            if( AuthMethod == AuthMethod.Token )
            {
                await InitTokenAuth();
                return;
            }

            throw new Exception( "Unexpected AuthMethod value" );
        }

        /// <summary>Request auth token, set options</summary>
        async Task InitTokenAuth()
        {
            CurrentToken = await Auth.Authorise( null, null, false );

            if( HasValidToken() )
            {
                // WebSockets use Token from Options.Token
                this.Options.Token = CurrentToken.Token;
                Logger.Debug( "Adding Authorization header with Token authentication" );
            }
            else
                throw new AblyException( "Invalid token credentials: " + CurrentToken, 40100, HttpStatusCode.Unauthorized );
        }

        /// <summary>
        /// This simply calls connection.close. Causes the connection to close, entering the closed state. Once
        /// closed, the library will not attempt to re-establish the connection without a call to connect().
        /// </summary>
        public void Close()
        {
            this.Connection.Close();
        }

        /// <summary>Retrieves the ably service time</summary>
        public Task<DateTime> Time()
        {
            return _simpleRest.Time();
        }
    }
}