using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionConnectedState : ConnectionState
    {
        private readonly ConnectionInfo _info;
        private bool _resumed = false;
        public ConnectionConnectedState(IConnectionContext context, ConnectionInfo info) :
            base(context)
        {
            _info = info;
        }

        public override ConnectionStateType State => ConnectionStateType.Connected;

        public override bool CanSend => true;

        public override void Close()
        {
            Context.SetState(new ConnectionClosingState(Context));
        }

        public override async Task<bool> OnMessageReceived(ProtocolMessage message)
        {
            switch (message.action)
            {
                case ProtocolMessage.MessageAction.Close:
                    Context.SetState(new ConnectionClosedState(Context, message.error));
                    return true;
                case ProtocolMessage.MessageAction.Disconnected:
                    var error = message.error;
                    var result = await Context.RetryBecauseOfTokenError(error);
                    if (result == false)
                        Context.SetState(new ConnectionDisconnectedState(Context, message.error));

                    return true;
                case ProtocolMessage.MessageAction.Error:
                    Context.SetState(new ConnectionFailedState(Context, message.error));
                    return true;
            }
            return false;
        }

        public override void AbortTimer()
        {
            
        }

        public override Task BeforeTransition()
        {
            Context.ResetConnectionAttempts();
            
            if (_info != null)
            {
                _resumed = Context.Connection.Id == _info.ConnectionId;
                Context.Connection.Id = _info.ConnectionId;
                Context.Connection.Key = _info.ConnectionKey;
                Context.Connection.Serial = _info.ConnectionSerial;
                if (_info.ConnectionStateTtl.HasValue)
                    Context.Connection.ConnectionStateTtl = _info.ConnectionStateTtl.Value;
                Context.SetConnectionClientId(_info.ClientId);
            }
            return base.BeforeTransition();
        }

        public override Task OnAttachToContext()
        {
            Context.ResetConnectionAttempts();
            Context.SendPendingMessages(_resumed);

            return TaskConstants.BooleanTrue;
        }
    }
}