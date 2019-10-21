using System.Threading.Tasks;
using IO.Ably;
using IO.Ably.MessageEncoders;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Transport;
using IO.Ably.Types;

namespace IO.Ably.Realtime
{
    internal class ChannelMessageProcessor
    {
        internal ILogger Logger { get; private set; }

        private IChannels<IRealtimeChannel> _channels;

        public ChannelMessageProcessor(IChannels<IRealtimeChannel> channels, ILogger logger)
        {
            Logger = logger;
            _channels = channels;
        }

        private RealtimeChannel GetChannel(string name)
        {
            return _channels.Get(name) as RealtimeChannel;
        }

        public Task<bool> MessageReceived(ProtocolMessage protocolMessage, RealtimeState state)
        {
            if (protocolMessage.Channel.IsEmpty())
            {
                return Task.FromResult(false);
            }

            var channel = _channels.Exists(protocolMessage.Channel) ? GetChannel(protocolMessage.Channel) : null;
            if (channel == null)
            {
                Logger.Warning($"Message received {protocolMessage} for a channel that does not exist {protocolMessage.Channel}");
                return Task.FromResult(false);
            }

            switch (protocolMessage.Action)
            {
                case ProtocolMessage.MessageAction.Error:
                    channel.SetChannelState(ChannelState.Failed, protocolMessage);
                    break;
                case ProtocolMessage.MessageAction.Attach:
                case ProtocolMessage.MessageAction.Attached:
                    if (channel.State == ChannelState.Attached)
                    {
                        // RTL12
                        if (!protocolMessage.HasFlag(ProtocolMessage.Flag.Resumed))
                        {
                            channel.EmitUpdate(protocolMessage.Error, false);
                        }
                    }
                    else
                    {
                        channel.SetChannelState(ChannelState.Attached, protocolMessage);
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
                    var result = MessageHandler.DecodeProtocolMessage(protocolMessage, channel.Options);
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
                    MessageHandler.DecodeProtocolMessage(protocolMessage, channel.Options);
                    channel.Presence.OnPresence(protocolMessage.Presence, null);
                    break;
                case ProtocolMessage.MessageAction.Sync:
                    MessageHandler.DecodeProtocolMessage(protocolMessage, channel.Options);
                    channel.Presence.OnPresence(protocolMessage.Presence, protocolMessage.ChannelSerial);
                    break;
            }

            return Task.FromResult(true);
        }
    }
}
