using System.Threading.Tasks;

using IO.Ably;
using IO.Ably.CustomSerialisers;
using IO.Ably.Realtime;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionConnectedState : ConnectionStateBase
    {
        private readonly ConnectionInfo _info;
        private bool? _resumed = null;

        public ConnectionConnectedState(IConnectionContext context, ConnectionInfo info, ErrorInfo error = null, ILogger logger = null)
            : base(context, logger)
        {
            _info = info;
            Error = error;
        }

        public override ConnectionState State => ConnectionState.Connected;

        public override bool CanSend => true;

        public override void Close()
        {
            Context.ExecuteCommand(SetClosingStateCommand.Create());
        }

        public override async ValueTask<bool> OnMessageReceived(ProtocolMessage message, RealtimeState state)
        {
            switch (message.Action)
            {
                case ProtocolMessage.MessageAction.Auth:
                    Context.ExecuteCommand(RetryAuthCommand.Create(false));
                    return true;
                case ProtocolMessage.MessageAction.Connected:
                    Context.ExecuteCommand(SetConnectedStateCommand.Create(message, isUpdate: true));
                    return true;
                case ProtocolMessage.MessageAction.Close:
                    Context.ExecuteCommand(new SetClosedStateCommand(message.Error));
                    return true;
                case ProtocolMessage.MessageAction.Disconnected:
                    if (await Context.CanUseFallBackUrl(message.Error))
                    {
                        Context.Connection.Key = null;
                        Context.ExecuteCommand(SetDisconnectedStateCommand.Create(message.Error, retryInstantly: true));

                        //Question: Should we fall through here or return
                    }

                    if (message.Error?.IsTokenError ?? false)
                    {
                        if (Context.ShouldWeRenewToken(message.Error))
                        {
                            Context.ExecuteCommand(RetryAuthCommand.Create(message.Error, true));
                        }
                        else
                        {
                            Context.ExecuteCommand(SetFailedStateCommand.Create(message.Error));
                        }

                        return true;
                    }

                    Context.ExecuteCommand(SetDisconnectedStateCommand.Create(message.Error));
                    return true;
                case ProtocolMessage.MessageAction.Error:
                    // an error message may signify an error state in the connection or in a channel
                    // Only handle connection errors here.
                    if (message.Channel.IsEmpty())
                    {
                        Context.ExecuteCommand(SetFailedStateCommand.Create(message.Error));
                    }

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
                if (WasThereAPreviousConnection())
                {
                    _resumed = Context.Connection.Id == _info.ConnectionId;
                }

                Context.Connection.Id = _info.ConnectionId;
                Context.Connection.Key = _info.ConnectionKey;
                Context.Connection.Serial = _info.ConnectionSerial;
                if (_info.ConnectionStateTtl.HasValue)
                {
                    Context.Connection.ConnectionStateTtl = _info.ConnectionStateTtl.Value;
                }

                Context.SetConnectionClientId(_info.ClientId);
            }

            if (_resumed.HasValue && _resumed.Value && Logger.IsDebug)
            {
                Logger.Debug("Connection resumed!");
            }
        }

        private bool WasThereAPreviousConnection()
        {
            return Context.Connection.Key.IsNotEmpty();
        }

        public override Task OnAttachToContext()
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Processing Connected:OnAttached. Resumed: " + _resumed);
            }

            if (_resumed == false)
            {
                Context.ClearAckQueueAndFailMessages(null);
                Context.DetachAttachedChannels(Error);
            }

            Context.SendPendingMessages(_resumed.GetValueOrDefault());
            return TaskConstants.BooleanTrue;
        }
    }
}
