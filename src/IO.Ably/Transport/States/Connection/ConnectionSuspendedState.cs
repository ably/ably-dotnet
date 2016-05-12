using System;
using System.Threading.Tasks;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionSuspendedState : ConnectionState
    {
        //TODO: Make sure these come from config
        private readonly ICountdownTimer _timer;

        public ConnectionSuspendedState(IConnectionContext context) :
            this(context, null, new CountdownTimer("Suspended state timer"))
        {
        }

        public ConnectionSuspendedState(IConnectionContext context, ErrorInfo error) :
            this(context, error, new CountdownTimer("Suspended state timer"))
        {
        }

        public ConnectionSuspendedState(IConnectionContext context, ErrorInfo error, ICountdownTimer timer) :
            base(context)
        {
            _timer = timer;
            Error = error ?? ErrorInfo.ReasonSuspended;
            RetryIn = context.SuspendRetryTimeout;
        }

        public override Realtime.ConnectionStateType State => Realtime.ConnectionStateType.Suspended;

        protected override bool CanQueueMessages => false;

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

        public override void AbortTimer()
        {
            _timer.Abort();
        }

        public override Task OnAttachedToContext()
        {
            if(RetryIn.HasValue)
                _timer.Start(RetryIn.Value, OnTimeOut);
            return TaskConstants.BooleanTrue;
        }

        private void OnTimeOut()
        {
            Context.SetState(new ConnectionConnectingState(Context));
        }
    }
}