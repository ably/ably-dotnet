using Ably.Types;
using System;

namespace Ably.Transport.States.Connection
{
    internal class ConnectionSuspendedState : ConnectionState
    {
        public ConnectionSuspendedState(IConnectionContext context) :
            this(context, null, new CountdownTimer())
        { }

        public ConnectionSuspendedState(IConnectionContext context, ErrorInfo error) :
            this(context, error, new CountdownTimer())
        { }

        public ConnectionSuspendedState(IConnectionContext context, ErrorInfo error, ICountdownTimer timer) :
            base(context)
        {
            _timer = timer;
            this.Error = error ?? ErrorInfo.ReasonSuspended;
            this.RetryIn = ConnectTimeout;
        }

        public const int SuspendTimeout = 120 * 1000; // Time before a connection is considered suspended
        private const int ConnectTimeout = 120 * 1000; // Time to wait before retrying connection
        private ICountdownTimer _timer;

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
            this.context.SetState(new ConnectionClosedState(this.context));
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
            this._timer.Start(ConnectTimeout, this.OnTimeOut);
        }

        private void OnTimeOut()
        {
            this.context.SetState(new ConnectionConnectingState(this.context));
        }
    }
}
