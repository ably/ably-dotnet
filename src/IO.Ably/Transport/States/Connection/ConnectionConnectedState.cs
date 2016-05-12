using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionConnectedState : ConnectionState
    {
        public ConnectionConnectedState(IConnectionContext context, ConnectionInfo info) :
            base(context)
        {
            Context.Connection.Id = info.ConnectionId;
            Context.Connection.Key = info.ConnectionKey;
            Context.Connection.Serial = info.ConnectionSerial;
            if (info.ConnectionStateTtl.HasValue)
                Context.Connection.ConnectionStateTtl = info.ConnectionStateTtl.Value;
            Context.SetConnectionClientId(info.ClientId);
        }

        public override ConnectionStateType State => ConnectionStateType.Connected;

        public override bool CanQueueMessages => false;
        public override bool CanSend => true;

        public override void Connect()
        {
            // do nothing
        }

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

        public override void SendMessage(ProtocolMessage message)
        {
            Context.Send(message);
        }

        public override Task OnAttachedToContext()
        {
            Context.ResetConnectionAttempts();

            if (Context.QueuedMessages != null && Context.QueuedMessages.Count > 0)
            {
                foreach (var message in Context.QueuedMessages)
                {
                    SendMessage(message);
                }
                Context.QueuedMessages.Clear();
            }

            return TaskConstants.BooleanTrue;
        }
    }
}