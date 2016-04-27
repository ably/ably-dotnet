using System.Threading.Tasks;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionClosedState : ConnectionState
    {
        public ConnectionClosedState(IConnectionContext context) :
            this(context, null)
        {
        }

        public ConnectionClosedState(IConnectionContext context, ErrorInfo error) :
            base(context)
        {
            Error = error ?? ErrorInfo.ReasonClosed;
        }

        public override Realtime.ConnectionStateType State => Realtime.ConnectionStateType.Closed;

        protected override bool CanQueueMessages => false;

        public override void Connect()
        {
            Context.SetState(new ConnectionConnectingState(Context));
        }

        public override void Close()
        {
            // do nothing
        }

        public override Task<bool> OnMessageReceived(ProtocolMessage message)
        {
            // could not happen
            return TaskConstants.BooleanFalse;
        }

        public override Task OnTransportStateChanged(TransportStateInfo state)
        {
            // could not happen
            return TaskConstants.BooleanTrue;
        }

        public override void OnAttachedToContext()
        {
            // This is a terminal state. Clear the transport.
            Context.DestroyTransport();
            Context.Connection.Key = null;
        }
    }
}