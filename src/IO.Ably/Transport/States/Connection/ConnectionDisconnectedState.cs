using System;
using System.Threading.Tasks;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionDisconnectedState : ConnectionState
    {
        private readonly ICountdownTimer _timer;

        public ConnectionDisconnectedState(IConnectionContext context) :
            this(context, null, new CountdownTimer("Disconnected state timer"))
        {
        }

        public ConnectionDisconnectedState(IConnectionContext context, TransportStateInfo stateInfo) :
            this(context, CreateError(stateInfo), new CountdownTimer("Disconnected state timer"))
        {
        }

        public ConnectionDisconnectedState(IConnectionContext context, ErrorInfo error) :
            this(context, error, new CountdownTimer("Disconnected state timer"))
        {
        }

        public ConnectionDisconnectedState(IConnectionContext context, ErrorInfo error, ICountdownTimer timer) :
            base(context)
        {
            _timer = timer;
            Error = error;
            RetryIn = context.RetryTimeout;
        }

        public bool RetryInstantly { get; set; }

        public override Realtime.ConnectionStateType State => Realtime.ConnectionStateType.Disconnected;

        protected override bool CanQueueMessages => true;

        public override void Connect()
        {
            Context.SetState(new ConnectionConnectingState(Context));
        }

        public override void Close()
        {
            _timer.Abort();
            Context.SetState(new ConnectionClosedState(Context));
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
            Logger.Error("Unexpected state change. " + state);
            return TaskConstants.BooleanTrue;
        }

        public override Task OnAttachedToContext()
        {
            if (RetryInstantly)
            {
                Context.SetState(new ConnectionConnectingState(Context));
            }  
            else
            {
                _timer.Start(Context.RetryTimeout, OnTimeOut);
            }

            return TaskConstants.BooleanTrue;
        }

        private void OnTimeOut()
        {
            Context.SetState(new ConnectionConnectingState(Context));
        }

        private static ErrorInfo CreateError(TransportStateInfo state)
        {
            return ErrorInfo.ReasonDisconnected;
        }
    }
}