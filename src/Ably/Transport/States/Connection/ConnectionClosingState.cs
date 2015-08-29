using Ably.Types;
using System;

namespace Ably.Transport.States.Connection
{
    internal class ConnectionClosingState : ConnectionState
    {
        public ConnectionClosingState(IConnectionContext context) :
            base(context)
        { }

        private const int CloseTimeout = 1000;
        private System.Threading.Timer _timer;

        public override Realtime.ConnectionState State
        {
            get
            {
                return Realtime.ConnectionState.Closing;
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
            // do nothing
        }

        public override void Close()
        {
            // do nothing
        }

        public override bool OnMessageReceived(ProtocolMessage message)
        {
            if (message.Action == ProtocolMessage.MessageAction.Closed)
            {
                StopTimer();
                this.context.SetState(new ConnectionClosedState(this.context));
                return true;
            }
            else if (message.Action == ProtocolMessage.MessageAction.Error)
            {
                StopTimer();
                this.context.SetState(new ConnectionFailedState(this.context, message.Error));
                return true;
            }
            return false;
        }

        public override void OnTransportStateChanged(TransportStateInfo state)
        {
            if (state.State == TransportState.Closed)
            {
                StopTimer();
                this.context.SetState(new ConnectionClosedState(this.context));
            }
        }

        public override void OnAttachedToContext()
        {
            if (this.context.Transport.State == TransportState.Connected)
            {
                this.context.Transport.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Close));
                StartTimer();
            }
            else
            {
                this.context.SetState(new ConnectionClosedState(this.context));
            }
        }

        private void StartTimer()
        {
            _timer = new System.Threading.Timer(OnTimeOut, null, CloseTimeout, System.Threading.Timeout.Infinite);
        }

        private void StopTimer()
        {
            if (_timer != null)
            {
                _timer.Dispose();
            }
        }

        private void OnTimeOut(object o)
        {
            this.context.SetState(new ConnectionClosedState(this.context));
        }
    }
}
