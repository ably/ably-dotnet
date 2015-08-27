using Ably.Types;
using System;

namespace Ably.Transport.States.Connection
{
    internal class ConnectionSuspendedState : ConnectionState
    {
        public ConnectionSuspendedState(IConnectionContext context) :
            base(context)
        { }

        public override Realtime.ConnectionState State
        {
            get
            {
                return Realtime.ConnectionState.Suspended;
            }
        }

        protected override bool CanQueueMessages
        {
            get
            {
                return false;
            }
        }

        public override void Connect()
        {
            this.context.SetState(new ConnectionConnectingState(this.context));
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
    }
}
