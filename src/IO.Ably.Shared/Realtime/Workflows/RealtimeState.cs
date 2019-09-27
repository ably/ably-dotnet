using System;
using System.Collections.Generic;

namespace IO.Ably.Realtime.Workflow
{
    internal class RealtimeState
    {
        public class ConnectionState
        {
            public Guid ConnectionId { get; } = Guid.NewGuid(); // Used to identify the connection for Os Event subscribers
            public DateTimeOffset? ConfirmedAliveAt { get; set; }

            /// <summary>
            ///     The id of the current connection. This string may be
            ///     used when recovering connection state.
            /// </summary>
            public string Id { get; set; }

            /// <summary>
            ///     The serial number of the last message received on this connection.
            ///     The serial number may be used when recovering connection state.
            /// </summary>
            public long? Serial { get; set; }

            internal long MessageSerial { get; set; } = 0;

            /// <summary>
            /// </summary>
            public string Key { get; set; }

            public TimeSpan ConnectionStateTtl { get; internal set; } = Defaults.ConnectionStateTtl;

            /// <summary>
            ///     Information relating to the transition to the current state,
            ///     as an Ably ErrorInfo object. This contains an error code and
            ///     message and, in the failed state in particular, provides diagnostic
            ///     error information.
            /// </summary>
            public ErrorInfo ErrorReason { get; set; }
        }

        public List<PingRequest> PingRequests { get; set; } = new List<PingRequest>();
        
        public ConnectionState Connection { get; private set; } = new ConnectionState();
    }
}