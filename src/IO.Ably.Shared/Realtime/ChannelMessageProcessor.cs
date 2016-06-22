using IO.Ably.Transport;
using IO.Ably.Types;

namespace IO.Ably.Realtime
{
    internal class ChannelMessageProcessor
    {
        private IRealtimeChannels _channels;
        private ConnectionManager _connectionManager;

        public ChannelMessageProcessor(ConnectionManager connectionManager, IRealtimeChannels channels)
        {
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
            if (protocolMessage.channel.IsEmpty())
                return;

            var channel = _channels.ContainsChannel(protocolMessage.channel) ? GetChannel(protocolMessage.channel) : null;
            if (channel == null)
            {
                Logger.Warning($"Message received {protocolMessage} for a channel that does not exist {protocolMessage.channel}");
                return;
            }

            switch (protocolMessage.action)
            {
                case ProtocolMessage.MessageAction.Error:
                    if (protocolMessage.channel.IsNotEmpty())
                    {
                        channel.SetChannelState(ChannelState.Failed, protocolMessage);
                    }
                    break;
                case ProtocolMessage.MessageAction.Attach:
                case ProtocolMessage.MessageAction.Attached:
                    if (channel.State != ChannelState.Attached)
                        channel.SetChannelState(ChannelState.Attached, protocolMessage);
                    else
                    {
                        if(protocolMessage.error != null)
                            channel.OnError(protocolMessage.error);
                    }
                    break;
                case ProtocolMessage.MessageAction.Detach:
                case ProtocolMessage.MessageAction.Detached:
                    if (channel.State != ChannelState.Detached)
                        channel.SetChannelState(ChannelState.Detached, protocolMessage);
                    break;
                case ProtocolMessage.MessageAction.Message:
                    var result = _connectionManager.Handler.DecodeProtocolMessage(protocolMessage, channel.Options);
                    if (result.IsFailure)
                    {
                        channel.OnError(result.Error);
                    }
                    foreach (var msg in protocolMessage.messages)
                    {
                        channel.OnMessage(msg);
                    }
                    break;
                case ProtocolMessage.MessageAction.Presence:
                    _connectionManager.Handler.DecodeProtocolMessage(protocolMessage, channel.Options);
                    channel.Presence.OnPresence(protocolMessage.presence, null);
                    break;
                case ProtocolMessage.MessageAction.Sync:
                    _connectionManager.Handler.DecodeProtocolMessage(protocolMessage, channel.Options);
                    channel.Presence.OnPresence(protocolMessage.presence, protocolMessage.channelSerial);
                    break;
            }
        }
    }
}