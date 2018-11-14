using System;
using IO.Ably.Types;

namespace IO.Ably.Transport
{
    public class ConnectionInfo
    {
        public static readonly ConnectionInfo Empty = new ConnectionInfo();

        private ConnectionInfo() { }

        public ConnectionInfo(string connectionId, long connectionSerial, string connectionKey, string clientId, TimeSpan? connectionStateTtl = null)
        {
            ClientId = clientId;
            ConnectionId = connectionId;
            ConnectionSerial = connectionSerial;
            ConnectionKey = connectionKey;
            ConnectionStateTtl = connectionStateTtl;
        }

        public ConnectionInfo(ProtocolMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message), "Null message");
            }

            if (message.Action != ProtocolMessage.MessageAction.Connected)
            {
                throw new InvalidOperationException(
                    $"A ConnectionInfo only be created from a Connected action protocol message. A value with action '{message.Action}' was passed" );
            }

            ConnectionId = message.ConnectionId;
            ConnectionSerial = message.ConnectionSerial ?? -1;
            ClientId = message.ConnectionDetails?.ClientId;
            ConnectionStateTtl = message.ConnectionDetails?.ConnectionStateTtl;
            ConnectionKey = message.ConnectionDetails?.ConnectionKey;
        }

        public TimeSpan? ConnectionStateTtl { get; private set; }

        public string ClientId { get; private set; }

        public string ConnectionId { get; private set; }

        public long ConnectionSerial { get; private set; }

        public string ConnectionKey { get; private set; }
    }
}