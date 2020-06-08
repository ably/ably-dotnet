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
        private readonly MessageHandler _messageHandler;

        public ChannelMessageProcessor(IChannels<IRealtimeChannel> channels, MessageHandler messageHandler, ILogger logger)
        {
            Logger = logger;
            _channels = channels;
            _messageHandler = messageHandler;
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
                case ProtocolMessage.MessageAction.Attached:
                    channel.Properties.AttachSerial = protocolMessage.ChannelSerial;
                    if (channel.State == ChannelState.Attached)
                    {
                        // RTL12
                        if (!protocolMessage.HasFlag(ProtocolMessage.Flag.Resumed))
                        {
                            channel.Presence.ChannelAttached(protocolMessage);
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
                    var result = _messageHandler.DecodeProtocolMessage(protocolMessage, channel.EncodingDecodingContext);
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
                    var presenceDecodeResult = _messageHandler.DecodeProtocolMessage(protocolMessage, channel.EncodingDecodingContext);
                    if (presenceDecodeResult.IsFailure)
                    {
                        channel.OnError(presenceDecodeResult.Error);
                    }
                    else
                    {
                        channel.Presence.OnPresence(protocolMessage.Presence, null);
                    }

                    break;
                case ProtocolMessage.MessageAction.Sync:
                    var decodeResult = _messageHandler.DecodeProtocolMessage(protocolMessage, channel.EncodingDecodingContext);
                    if (decodeResult.IsFailure)
                    {
                        channel.OnError(decodeResult.Error);
                    }
                    else
                    {
                        channel.Presence.OnPresence(protocolMessage.Presence, protocolMessage.ChannelSerial);
                    }

                    break;
            }

            return Task.FromResult(true);
        }
    }
}
