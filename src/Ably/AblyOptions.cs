using Ably.Transport;
using System;
using System.Collections.Generic;

namespace Ably
{
    /// <summary>
    /// Ably library options for REST and Realtime APIs
    /// </summary>
    public class AblyOptions : AuthOptions
    {
        public AblyOptions()
        {
            this.Transports = new HashSet<ITransport>();
            this.AutoConnect = true;
        }

        /// <summary>
        /// Construct an options with a single key string. The key string is obtained
        /// from the application dashboard.
        /// </summary>
        /// <param name="key">the key string</param>
        /// <exception cref="AblyException" />
        public AblyOptions(string key)
            : base(key)
        {
            this.Transports = new HashSet<ITransport>();
            this.AutoConnect = true;
        }

        /// <summary>
        /// The application id. This option is only required if the application id 
        /// cannot be inferred either from a key or token option. If given, it is the 
        /// application id as indicated on the application dashboard.
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// Indicates whether or not a TLS (“SSL”) secure connection should be used.
        /// </summary>
        public bool Tls { get; set; }

        public string Host { get; set; }

        public int? Port { get; set; }

        /// <summary>
        /// A client id, used for identifying this client for presence purposes. The clientId can be any string. 
        /// This option is primarily intended to be used in situations where the library is instanced with a 
        /// key; note that a clientId may also be implicit in a token used to instance the library; an error will 
        /// be raised if a clientId specified here conflicts with the clientId implicit in the token.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// An int controlling the verbosity of the output from 1 (minimum, errors only) to 4 (most verbose). 
        /// </summary>
        public byte LogLevel { get; set; }

        /// <summary>
        /// In those environments supporting multiple transports, an array of transports to use, in 
        /// descending order of preference.
        /// </summary>
        public ISet<ITransport> Transports { get; private set; }

        /// <summary>
        /// If <c>true</c>, forces the library to use the JSON encoding for REST and Realtime operations,
        /// instead of the default binary msgpack encoding.
        /// </summary>
        public bool UseTextProtocol { get; set; }

        /// <summary>
        /// If <c>false</c>, this disables the default behaviour whereby the library queues messages on a 
        /// connection in the disconnected or connecting states. The default behaviour allows applications 
        /// to submit messages immediately upon instancing the library, without having to wait for the 
        /// connection to be established. Applications may use this option to disable that behaviour if they 
        /// wish to have application-level control over the queueing under those conditions.
        /// </summary>
        public bool QueueMessages { get; set; }

        /// <summary>
        /// If <c>false</c>, prevents messages originating from this connection being echoed back on the same 
        /// connection.
        /// </summary>
        public bool EchoMessages { get; set; }

        /// <summary>
        /// A connection recovery string, specified by a client when initialising the library 
        /// with the intention of inheriting the state of an earlier connection. See the Ably 
        /// Realtime API documentation for further information on connection state recovery.
        /// </summary>
        public string Recover { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool AutoConnect { get; set; }

        public ChannelOptions ChannelDefaults { get; set; }
    }
}
