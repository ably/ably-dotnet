using System.Threading.Tasks;
using IO.Ably.CustomSerialisers;
using IO.Ably.Realtime;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionConnectedState : ConnectionStateBase
    {
        private readonly ConnectionInfo _info;
        private bool? _resumed = null;
        public ConnectionConnectedState(IConnectionContext context, ConnectionInfo info, ErrorInfo error = null) :
            base(context)
        {
            _info = info;
            Error = error;
        }

        public override ConnectionState State => ConnectionState.Connected;

        public override bool CanSend => true;

        public override void Close()
        {
            Context.SetState(new ConnectionClosingState(Context));
        }

        public override async Task<bool> OnMessageReceived(ProtocolMessage message)
        {
            switch (message.Action)
            {
                case ProtocolMessage.MessageAction.Close:
                    Context.SetState(new ConnectionClosedState(Context, message.Error));
                    return true;
                case ProtocolMessage.MessageAction.Disconnected:
                    var error = message.Error;
                    var result = await Context.RetryBecauseOfTokenError(error);
                    if (result == false)
                        Context.SetState(new ConnectionDisconnectedState(Context, message.Error));

                    return true;
                case ProtocolMessage.MessageAction.Error:
                    if (await Context.RetryBecauseOfTokenError(message.Error))
                        return true;
                    if (await Context.CanUseFallBackUrl(message.Error))
                    {
                        Context.Connection.Key = null;
                        Context.SetState(new ConnectionDisconnectedState(Context, message.Error) { RetryInstantly = true });
                        return true;
                    }

                    Context.SetState(new ConnectionFailedState(Context, message.Error));
                    return true;
            }
            return false;
        }

        public override void AbortTimer()
        {
            
        }

        public override void BeforeTransition()
        {
            if (_info != null)
            {
                if (Context.Connection.Key.IsNotEmpty() &&
                    Context.Connection.Id == _info.ConnectionId)
                {
                    _resumed = true;
                }
                
                Context.Connection.Id = _info.ConnectionId;
                Context.Connection.Key = _info.ConnectionKey;
                Context.Connection.Serial = _info.ConnectionSerial;
                if (_info.ConnectionStateTtl.HasValue)
                    Context.Connection.ConnectionStateTtl = _info.ConnectionStateTtl.Value;
                Context.SetConnectionClientId(_info.ClientId);
            }

            if(_resumed.HasValue && _resumed.Value && Logger.IsDebug) Logger.Debug("Connection resumed!");
        }

        public override Task OnAttachToContext()
        {
            if(Logger.IsDebug) Logger.Debug("Processing Connected:OnAttached. Resumed: " + _resumed);
            if (_resumed != true)
            {
                Context.ClearAckQueueAndFailMessages(null);
            }
            else if(_resumed.Value == false)
                Context.DetachAttachedChannels(Error);

            Context.SendPendingMessages(_resumed.GetValueOrDefault());
            return TaskConstants.BooleanTrue;
        }
    }
}