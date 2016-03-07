using System;
using System.Collections.Generic;

namespace Ably
{
    /// <summary>
    /// Ably options class. It is used to instantiate Ably.Realtime.Client
    /// </summary>
    public class AblyRealtimeOptions : AblyOptions
    {
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
        /// A connection recovery string, specified by a client when initializing the library 
        /// with the intention of inheriting the state of an earlier connection. See the Ably 
        /// Realtime API documentation for further information on connection state recovery.
        /// </summary>
        public string Recover { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool AutoConnect { get; set; }

        /// <summary>
        /// Defaul constructor for AblyRealtimeOptions
        /// </summary>
        public AblyRealtimeOptions()
        {
            this.AutoConnect = true;
            this.EchoMessages = true;
            this.QueueMessages = true;
        }

        /// <summary>
        /// Construct AblyOptions class and set the Key
        /// It automatically parses the key to ensure the correct format is used and sets the KeyId and KeyValue properties
        /// </summary>
        /// <param name="key">Ably authentication key</param>
        public AblyRealtimeOptions(string key) : base(key)
        {
            this.AutoConnect = true;
            this.EchoMessages = true;
            this.QueueMessages = true;
        }
    }
}
