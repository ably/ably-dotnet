﻿using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Rest;
using IO.Ably.Transport;

namespace IO.Ably
{
    /// <summary>
    ///
    /// </summary>
    public class AblyRealtime : AblyBase
    {
        /// <summary></summary>
        /// <param name="key"></param>
        public AblyRealtime(string key)
            : this(new AblyRealtimeOptions(key))
        { }

        /// <summary>
        ///
        /// </summary>
        /// <param name="options"></param>
        public AblyRealtime(AblyRealtimeOptions options)
        {
            _options = options;
            if (options.AutoConnect)
                this.Connect();
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
        public Connection Connect()
        {
            if( null == this.Channels || null == this.Connection )
            {
                _simpleRest = new Rest.AblySimpleRestClient( _options );
                InitAuth( _simpleRest );

                IConnectionManager connectionManager = new ConnectionManager( options );
                _protocol = _options.UseBinaryProtocol == false ? Protocol.Json : Protocol.MsgPack;
                IChannelFactory factory = new ChannelFactory() { ConnectionManager = connectionManager, Options = options };
                this.Channels = new ChannelList( connectionManager, factory );
                this.Connection = connectionManager.Connection;
            }

            this.Connection.Connect();
            return this.Connection;
        }

        new internal void InitAuth( IAblyRest restClient )
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
                InitTokenAuth();
                return;
            }

            throw new Exception( "Unexpected AuthMethod value" );
        }

        /// <summary>Request auth token, set options</summary>
        void InitTokenAuth()
        {
            CurrentToken = Auth.Authorise( null, null, false );

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

        /// <summary>
        /// Retrieves the ably service time
        /// </summary>
        /// <returns></returns>
        public void Time(Action<DateTimeOffset?, AblyException> callback)
        {
            System.Threading.SynchronizationContext sync = System.Threading.SynchronizationContext.Current;

            Action<DateTimeOffset?, AblyException> invokeCallback = (res, err) =>
            {
                if (callback != null)
                {
                    if (sync != null)
                    {
                        sync.Send(new SendOrPostCallback(o => callback(res, err)), null);
                    }
                    else
                    {
                        callback(res, err);
                    }
                }
            };

            Action act = () =>
            {
                DateTimeOffset result;
                try
                {
                    result = _simpleRest.Time();
                }
                catch (AblyException e)
                {
                    invokeCallback(null, e);
                    return;
                }
                invokeCallback(result, null);
            };
            Task.Run( act );
        }
    }
}