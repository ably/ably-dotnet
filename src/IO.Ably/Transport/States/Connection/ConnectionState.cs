using System;
using System.Diagnostics;
using System.Threading.Tasks;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    [DebuggerDisplay("{State}")]
    internal abstract class ConnectionState
    {
        internal ConnectionState() { }

        public ConnectionState(IConnectionContext context)
        {
            this.Context = context;
        }

        protected readonly IConnectionContext Context;

        public abstract Realtime.ConnectionStateType State { get; }
        public ErrorInfo Error { get; protected set; }
        public TimeSpan? RetryIn { get; protected set; }

        protected abstract bool CanQueueMessages { get; }

        public abstract void Connect();
        public abstract void Close();
        public abstract Task OnTransportStateChanged(TransportStateInfo state);
        public abstract Task<bool> OnMessageReceived(ProtocolMessage message);
        public abstract void AbortTimer();

        public virtual Task OnAttachedToContext()
        {
            return TaskConstants.BooleanTrue;
        }

        public virtual void SendMessage(ProtocolMessage message)
        {
            if (CanQueueMessages)
            {
                Context.QueuedMessages.Enqueue(message);
            }
        }

        public override string ToString()
        {
            return $"Type: {GetType().Name}, State: {State}";
        }

        public class TransportStateInfo
        {
            public TransportStateInfo(TransportState state)
                : this(state, null)
            { }

            public TransportStateInfo(TransportState state, Exception error)
            {
                State = state;
                Error = error;
            }

            public TransportState State { get; private set; }
            public Exception Error { get; private set; }

            public override string ToString()
            {
                return $"State: {State}. Error: {Error}";
            }
        }
    }
}
