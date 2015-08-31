using Ably.Types;
using System;

namespace Ably.Transport.States.Connection
{
    internal class ConnectionDisconnectedState : ConnectionState
    {
        public ConnectionDisconnectedState(IConnectionContext context, TransportStateInfo transportState) :
            this(context, transportState, new CountdownTimer())
        { }

        public ConnectionDisconnectedState(IConnectionContext context, TransportStateInfo transportState, ICountdownTimer timer) :
            base(context)
        {
            this.Error = CreateError(transportState);
            _timer = timer;
        }

        private const int ConnectTimeout = 15 * 1000;
        private ICountdownTimer _timer;

        public ConnectionDisconnectedState(IConnectionContext context, ErrorInfo error) :
            base(context)
        {
            this.Error = error;
        }

        public override Realtime.ConnectionState State
        {
            get
            {
                return Realtime.ConnectionState.Disconnected;
            }
        }

        protected override bool CanQueueMessages
        {
            get
            {
                return true;
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

        private static ErrorInfo CreateError(TransportStateInfo state)
        {
            return ErrorInfo.ReasonDisconnected;
        }
    }
}
