using System;
using System.Diagnostics;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    [DebuggerDisplay("{State}")]
    internal abstract class ConnectionState
    {
        internal ConnectionState() { }

        public ConnectionState(IConnectionContext context)
        {
            this.context = context;
        }

        protected IConnectionContext context;

        public abstract Realtime.ConnectionState State { get; }
        public ErrorInfo Error { get; protected set; }
        public int? RetryIn { get; protected set; }

        protected abstract bool CanQueueMessages { get; }

        public abstract void Connect();
        public abstract void Close();
        public abstract void OnTransportStateChanged(TransportStateInfo state);
        public abstract bool OnMessageReceived(ProtocolMessage message);

        public virtual void OnAttachedToContext()
        {
        }

        public virtual void SendMessage(ProtocolMessage message)
        {
            if (CanQueueMessages)
            {
                context.QueuedMessages.Enqueue(message);
            }
        }

        public class TransportStateInfo
        {
            public TransportStateInfo(TransportState state)
                : this(state, null)
            { }

            public TransportStateInfo(TransportState state, Exception error)
            {
                this.State = state;
                this.Error = error;
            }

            public TransportState State { get; private set; }
            public Exception Error { get; private set; }
        }
    }
}
