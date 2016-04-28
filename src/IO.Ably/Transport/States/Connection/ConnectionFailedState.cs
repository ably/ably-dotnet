using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionFailedState : ConnectionState
    {
        public ConnectionFailedState(IConnectionContext context, TransportStateInfo transportState) :
            base(context)
        {
            Error = CreateError(transportState);
        }

        public ConnectionFailedState(IConnectionContext context, ErrorInfo error) :
            base(context)
        {
            Error = error ?? ErrorInfo.ReasonFailed;
        }

        public override Realtime.ConnectionState State => Realtime.ConnectionState.Failed;

        protected override bool CanQueueMessages => false;

        public override void Connect()
        {
            context.SetState(new ConnectionConnectingState(context));
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
            context.DestroyTransport();
            context.Connection.Key = null;
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