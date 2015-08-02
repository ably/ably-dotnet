using Ably.CustomSerialisers;
using Ably.Realtime;
using Ably.Transport;
using Ably.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Ably
{
    /// <summary>
    /// 
    /// </summary>
    public class AblyRealtime : AblyBase
    {
        static AblyRealtime()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                Converters = new List<JsonConverter>()
                {
                    new DateTimeOffsetJsonConverter(),
                    new CapabilityJsonConverter()
                }
            };
        }


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
            _options = options;
            _protocol = _options.UseBinaryProtocol == false ? Protocol.Json : Protocol.MsgPack;
            IChannelFactory factory = new ChannelFactory() { ConnectionManager = connectionManager, Options = options };
            this.Channels = new ChannelList(connectionManager, factory);
            this.Connection = new Connection(connectionManager);
            InitAuth(new Rest.AblySimpleRestClient(options));

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
