using System;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionFailedState : ConnectionState
    {
        public ConnectionFailedState(IConnectionContext context, TransportStateInfo transportState) :
            base(context)
        {
            this.Error = CreateError(transportState);
        }

        public ConnectionFailedState(IConnectionContext context, ErrorInfo error) :
            base(context)
        {
            this.Error = error ?? ErrorInfo.ReasonFailed;
        }

        public override Realtime.ConnectionState State
        {
            get
            {
                return Realtime.ConnectionState.Failed;
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
            // does nothing
        }

        public override bool OnMessageReceived(ProtocolMessage message)
        {
            // could not happen
            return false;
        }

        public override void OnTransportStateChanged(TransportStateInfo state)
        {
            // could not happen
        }

        public override void OnAttachedToContext()
        {
            // This is a terminal state. Clear the transport.
            this.context.DestroyTransport();
            this.context.Connection.Key = null;
        }

        private static ErrorInfo CreateError(TransportStateInfo state)
        {
            if (state != null && state.Error != null)
            {
                if (state.Error.Message == "HTTP/1.1 401 Unauthorized")
                    return ErrorInfo.ReasonRefused;
            }
            return ErrorInfo.ReasonFailed;
        }
    }
}
