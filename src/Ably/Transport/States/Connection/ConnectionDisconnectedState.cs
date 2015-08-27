using Ably.Types;
using System;

namespace Ably.Transport.States.Connection
{
    internal class ConnectionDisconnectedState : ConnectionState
    {
        public ConnectionDisconnectedState(IConnectionContext context, TransportStateInfo transportState) :
            base(context)
        {
            this.Error = CreateError(transportState);
        }

        public ConnectionDisconnectedState(IConnectionContext context, ErrorInfo error) :
            base(context)
        {
            this.Error = error;
        }

        public override Realtime.ConnectionState State
        {
            get
            {
                return Realtime.ConnectionState.Disconnected;
            }
        }

        protected override bool CanQueueMessages
        {
            get
            {
                return true;
            }
        }

        public override void Connect()
        {
            // do nothing
        }

        public override void Close()
        {
            throw new NotImplementedException();
        }

        public override bool OnMessageReceived(ProtocolMessage message)
        {
            throw new NotImplementedException();
        }

        public override void OnTransportStateChanged(TransportStateInfo state)
        {
            throw new NotImplementedException();
        }

        private static ErrorInfo CreateError(TransportStateInfo state)
        {
            return ErrorInfo.ReasonDisconnected;
        }
    }
}
