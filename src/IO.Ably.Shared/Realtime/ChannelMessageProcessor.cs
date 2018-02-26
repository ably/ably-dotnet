using IO.Ably;
using IO.Ably.Transport;
using IO.Ably.Types;

namespace IO.Ably.Realtime
{
    internal class ChannelMessageProcessor
    {
        internal ILogger Logger { get; private set; }
        private IChannels<IRealtimeChannel> _channels;
        private ConnectionManager _connectionManager;

        public ChannelMessageProcessor(ConnectionManager connectionManager, IChannels<IRealtimeChannel> channels)
        {
            Logger = connectionManager.Logger;
            _connectionManager = connectionManager;
            _channels = channels;
            _connectionManager.MessageReceived += MessageReceived;
        }

        private RealtimeChannel GetChannel(string name)
        {
            return _channels.Get(name) as RealtimeChannel;
        }

        private void MessageReceived(ProtocolMessage protocolMessage)
        {
            if (protocolMessage.Channel.IsEmpty())
            {
                return;
            }

            var channel = _channels.Exists(protocolMessage.Channel) ? GetChannel(protocolMessage.Channel) : null;
            if (channel == null)
            {
                Logger.Warning($"Message received {protocolMessage} for a channel that does not exist {protocolMessage.Channel}");
                return;
            }

            switch (protocolMessage.Action)
            {
                case ProtocolMessage.MessageAction.Error:
                    if (protocolMessage.Channel.IsNotEmpty())
                    {
                        channel.SetChannelState(ChannelState.Failed, protocolMessage);
                    }

                    break;
                case ProtocolMessage.MessageAction.Attach:
                case ProtocolMessage.MessageAction.Attached:
                    if (channel.State != ChannelState.Attached)
                    {
                        channel.SetChannelState(ChannelState.Attached, protocolMessage);
                    }
                    else
                    {
                        if(protocolMessage.Error != null)
                        {
                            channel.OnError(protocolMessage.Error);
                        }
                    }

                    break;
                case ProtocolMessage.MessageAction.Detach:
                case ProtocolMessage.MessageAction.Detached:
                    if (channel.State != ChannelState.Detached)
                    {
                        channel.SetChannelState(ChannelState.Detached, protocolMessage);
                    }

                    break;
                case ProtocolMessage.MessageAction.Message:
                    var result = _connectionManager.Handler.DecodeProtocolMessage(protocolMessage, channel.Options);
                    if (result.IsFailure)
                    {
                        channel.OnError(result.Error);
                    }

                    foreach (var msg in protocolMessage.Messages)
                    {
                        channel.OnMessage(msg);
                    }

                    break;
                case ProtocolMessage.MessageAction.Presence:
                    _connectionManager.Handler.DecodeProtocolMessage(protocolMessage, channel.Options);
                    channel.Presence.OnPresence(protocolMessage.Presence, null);
                    break;
                case ProtocolMessage.MessageAction.Sync:
                    _connectionManager.Handler.DecodeProtocolMessage(protocolMessage, channel.Options);
                    channel.Presence.OnPresence(protocolMessage.Presence, protocolMessage.ChannelSerial);
                    break;
            }


        }
    }
}