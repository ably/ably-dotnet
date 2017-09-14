using System;
using System.Diagnostics;
using System.Threading.Tasks;
using IO.Ably;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    [DebuggerDisplay("{State}")]
    internal abstract class ConnectionStateBase
    {
        internal ILogger Logger { get; private set; }

        protected ConnectionStateBase(IConnectionContext context, ILogger logger)
        {
            Logger = logger ?? IO.Ably.Logger.LoggerInstance;
            Context = context;
        }

        protected readonly IConnectionContext Context;
        public abstract Realtime.ConnectionState State { get; }
        public ErrorInfo Error { get; protected set; }
        
        public Exception Exception { get; set; }
        public TimeSpan? RetryIn { get; protected set; }
        public virtual bool CanQueue => false;
        public virtual bool CanSend => false;

        public virtual void Connect()
        {
        }

        public virtual void Close()
        {
        }

        public virtual Task<bool> OnMessageReceived(ProtocolMessage message)
        {
            return TaskConstants.BooleanFalse;
        }

        public virtual void AbortTimer()
        {

        }

        public virtual void BeforeTransition()
        {
        }

        public virtual Task OnAttachToContext()
        {
            return TaskConstants.BooleanTrue;
        }

        public override string ToString()
        {
            return $"Type: {GetType().Name}, State: {State}";
        }
    }
}
