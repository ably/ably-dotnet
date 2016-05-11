using System;
using System.Threading.Tasks;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionClosingState : ConnectionState
    {
        private const int CloseTimeout = 1000;
        private readonly ICountdownTimer _timer;

        public ConnectionClosingState(IConnectionContext context) :
            this(context, null, new CountdownTimer("Closing state timer"))
        {
        }

        public ConnectionClosingState(IConnectionContext context, ErrorInfo error) :
            this(context, error, new CountdownTimer("Closing state timer"))
        {
        }

        public ConnectionClosingState(IConnectionContext context, ErrorInfo error, ICountdownTimer timer) :
            base(context)
        {
            _timer = timer;
            Error = error ?? ErrorInfo.ReasonClosed;
        }

        public override Realtime.ConnectionStateType State => Realtime.ConnectionStateType.Closing;

        protected override bool CanQueueMessages => false;

        public override void Connect()
        {
            //do nothing
        }

        public override void Close()
        {
            // do nothing
        }

        public override Task<bool> OnMessageReceived(ProtocolMessage message)
        {
            switch (message.action)
            {
                case ProtocolMessage.MessageAction.Closed:
                    {
                        TransitionState(new ConnectionClosedState(Context));
                        return TaskConstants.BooleanTrue;
                    }
                case ProtocolMessage.MessageAction.Disconnected:
                    {
                        TransitionState(new ConnectionDisconnectedState(Context, message.error));
                        return TaskConstants.BooleanTrue;
                    }
                case ProtocolMessage.MessageAction.Error:
                    {
                        TransitionState(new ConnectionFailedState(Context, message.error));
                        return TaskConstants.BooleanTrue;
                    }
            }
            return TaskConstants.BooleanFalse;
        }

        public override void AbortTimer()
        {
            _timer.Abort();
        }

        public override Task OnTransportStateChanged(TransportStateInfo state)
        {
            if (state.State == TransportState.Closed)
            {
                TransitionState(new ConnectionClosedState(Context));
            }
            return TaskConstants.BooleanTrue;
        }

        public override Task OnAttachedToContext()
        {
            if (Context.TransportState == TransportState.Connected)
            {
                Context.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Close));
                _timer.Start(TimeSpan.FromMilliseconds(CloseTimeout), () => Context.SetState(new ConnectionClosedState(Context)));
            }
            else
            {
                Context.SetState(new ConnectionClosedState(Context));
            }
            return TaskConstants.BooleanTrue;
        }

        private void TransitionState(ConnectionState newState)
        {
            Context.SetState(newState);
            _timer.Abort();
        }
    }
}