using IO.Ably.Transport;
using IO.Ably.Types;

namespace IO.Ably.Realtime
{
    internal class ChannelMessageProcessor
    {
        private IRealtimeChannelCommands _channels;
        private ConnectionManager _connectionManager;

        public ChannelMessageProcessor(ConnectionManager connectionManager, IRealtimeChannelCommands channels)
        {
            _connectionManager = connectionManager;
            _channels = channels;
            _connectionManager.MessageReceived += MessageReceived;
        }

        private RealtimeChannel GetChannel(string name)
        {
            return _channels.Get(name) as RealtimeChannel;
        }

        private void MessageReceived(ProtocolMessage message)
        {
            if (message.channel.IsEmpty())
                return;

            var channel = _channels.ContainsChannel(message.channel) ? GetChannel(message.channel) : null;
            if (channel == null)
            {
                Logger.Warning($"Message received {message} for a channel that does not exist {message.channel}");
                return;
            }

            switch (message.action)
            {
                case ProtocolMessage.MessageAction.Error:
                    if (message.channel.IsNotEmpty())
                    {
                        channel.SetChannelState(ChannelState.Failed, message);
                    }
                    break;
                case ProtocolMessage.MessageAction.Attach:
                case ProtocolMessage.MessageAction.Attached:
                    if (channel.State != ChannelState.Attached)
                        channel.SetChannelState(ChannelState.Attached, message);
                    break;
                case ProtocolMessage.MessageAction.Detach:
                case ProtocolMessage.MessageAction.Detached:
                    if (channel.State != ChannelState.Detached)
                        channel.SetChannelState(ChannelState.Detached, message);
                    break;
                case ProtocolMessage.MessageAction.Message:
                    foreach (var msg in message.messages)
                        channel.OnMessage(msg);
                    break;
                case ProtocolMessage.MessageAction.Presence:
                    channel.Presence.OnPresence(message.presence, null);
                    break;
                case ProtocolMessage.MessageAction.Sync:
                    channel.Presence.OnPresence(message.presence, message.channelSerial);
                    break;
            }
        }
    }
}