using Ably.Types;
using System;

namespace Ably.Transport.States.Connection
{
    internal class ConnectionDisconnectedState : ConnectionState
    {
        public ConnectionDisconnectedState(IConnectionContext context) :
            this(context, null, new CountdownTimer())
        { }

        public ConnectionDisconnectedState(IConnectionContext context, TransportStateInfo stateInfo) :
            this(context, CreateError(stateInfo), new CountdownTimer())
        { }

        public ConnectionDisconnectedState(IConnectionContext context, ErrorInfo error) :
            this(context, error, new CountdownTimer())
        { }

        public ConnectionDisconnectedState(IConnectionContext context, ErrorInfo error, ICountdownTimer timer) :
            base(context)
        {
            _timer = timer;
            this.Error = error ?? ErrorInfo.ReasonDisconnected;
            this.RetryIn = ConnectTimeout;
        }

        private const int ConnectTimeout = 15 * 1000;
        private ICountdownTimer _timer;

        public bool UseFallbackHost { get; set; }

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
            if (UseFallbackHost)
            {
                this.context.SetState(new ConnectionConnectingState(this.context));
            }
            else
            {
                this._timer.Start(ConnectTimeout, this.OnTimeOut);
            }
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
