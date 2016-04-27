using System;
using System.Threading.Tasks;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionSuspendedState : ConnectionState
    {
        //TODO: Make sure these come from config
        public const int SuspendTimeout = 120*1000; // Time before a connection is considered suspended
        private const int ConnectTimeout = 120*1000; // Time to wait before retrying connection
        private readonly ICountdownTimer _timer;

        public ConnectionSuspendedState(IConnectionContext context) :
            this(context, null, new CountdownTimer())
        {
        }

        public ConnectionSuspendedState(IConnectionContext context, ErrorInfo error) :
            this(context, error, new CountdownTimer())
        {
        }

        public ConnectionSuspendedState(IConnectionContext context, ErrorInfo error, ICountdownTimer timer) :
            base(context)
        {
            _timer = timer;
            Error = error ?? ErrorInfo.ReasonSuspended;
            RetryIn = ConnectTimeout;
        }

        public override Realtime.ConnectionStateType State => Realtime.ConnectionStateType.Suspended;

        protected override bool CanQueueMessages => false;

        public override void Connect()
        {
            Context.SetState(new ConnectionConnectingState(Context));
        }

        public override void Close()
        {
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
            Logger.Error("Unexpected state change. " + state.State);
            return TaskConstants.BooleanTrue;
        }

        public override Task OnAttachedToContext()
        {
            _timer.Start(TimeSpan.FromMilliseconds(ConnectTimeout), OnTimeOut);
            return TaskConstants.Completed;
        }

        private void OnTimeOut()
        {
            Context.SetState(new ConnectionConnectingState(Context));
        }
    }
}