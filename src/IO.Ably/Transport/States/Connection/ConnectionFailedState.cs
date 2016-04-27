using System.Threading.Tasks;
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

        public override Realtime.ConnectionStateType State => Realtime.ConnectionStateType.Failed;

        protected override bool CanQueueMessages => false;

        public override void Connect()
        {
            Context.SetState(new ConnectionConnectingState(Context));
        }

        public override void Close()
        {
            // does nothing
        }

        public override Task<bool> OnMessageReceived(ProtocolMessage message)
        {
            // could not happen
            Logger.Error("Receiving message in disconected state!");
            return TaskConstants.BooleanFalse;
        }

        public override Task OnTransportStateChanged(TransportStateInfo state)
        {
            // could not happen
            Logger.Error("Unexpected state change. " + state.State);
            return TaskConstants.BooleanTrue;
        }

        public override Task OnAttachedToContext()
        {
            // This is a terminal state. Clear the transport.
            Context.DestroyTransport();
            Context.Connection.Key = null;
            return TaskConstants.Completed;
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