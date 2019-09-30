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
        private bool _resumed;

        public ConnectionConnectedState(
                                        IConnectionContext context,
                                        ErrorInfo error = null,
                                        bool resumed = false,
                                        bool isUpdate = false,
                                        ILogger logger = null)
                                        : base(context, logger)
        {
            Error = error;
            IsUpdate = isUpdate;
            _resumed = resumed;
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

            Context.SendPendingMessages(_resumed);

            return TaskConstants.BooleanTrue;
        }
    }
}
