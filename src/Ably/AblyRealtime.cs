using Ably.Realtime;
using Ably.Transport;
using Ably.Types;
using System;
using System.Collections.Generic;

namespace Ably
{
    /// <summary>
    /// 
    /// </summary>
    public class AblyRealtime
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public AblyRealtime(string key)
            : this(new AblyRealtimeOptions(key))
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        public AblyRealtime(AblyRealtimeOptions options)
            : this(options, new ConnectionManager(options)) { }

        internal AblyRealtime(AblyRealtimeOptions options, IConnectionManager connectionManager)
        {
            IPresenceFactory factory = new PresenceFactory() { ConnectionManager = connectionManager, Options = options };
            this.Channels = new ChannelList(connectionManager, factory);
            this.Connection = new Connection(connectionManager);
            if (options.AutoConnect)
            {
                this.Connection.Connect();
            }
        }

        /// <summary>
        /// The collection of channels instanced, indexed by channel name.
        /// </summary>
        public IRealtimeChannelCommands Channels { get; private set; }

        /// <summary>
        /// A reference to the connection object for this library instance.
        /// </summary>
        public Connection Connection { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Connection Connect()
        {
            this.Connection.Connect();
            return this.Connection;
        }

        /// <summary>
        /// This simply calls connection.close. Causes the connection to close, entering the closed state. Once 
        /// closed, the library will not attempt to re-establish the connection without a call to connect().
        /// </summary>
        public void Close()
        {
            this.Connection.Close();
        }
    }
}
