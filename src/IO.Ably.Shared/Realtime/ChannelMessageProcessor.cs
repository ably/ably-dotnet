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
                    var result = _messageHandler.DecodeMessages(
                        protocolMessage,
                        protocolMessage.Messages,
                        channel.MessageDecodingContext);

                    if (result.IsFailure)
                    {
                        Logger.Error($"{channel.Name} - failed to decode message. ErrorCode: {result.Error.Code}, Message: {result.Error.Message}");
                        if (result.Error is VcdiffErrorInfo)
                        {
                            channel.StartDecodeFailureRecovery();

                            // Break any further message processing
                            return Task.FromResult(true);
                        }
                    }

                    channel.LastSuccessfulMessageIds = LastMessageIds.Create(protocolMessage);

                    foreach (var msg in protocolMessage.Messages)
                    {
                        channel.OnMessage(msg);
                    }

                    break;
                case ProtocolMessage.MessageAction.Presence:
                case ProtocolMessage.MessageAction.Sync:

                    var presenceDecodeResult = _messageHandler.DecodeMessages(
                                                protocolMessage,
                                                protocolMessage.Presence,
                                                channel.Options);

                    if (presenceDecodeResult.IsFailure)
                    {
                        Logger.Error($"{channel.Name} - failed to decode presence message. ErrorCode: {presenceDecodeResult.Error.Code}, Message: {presenceDecodeResult.Error.Message}");

                        channel.OnError(presenceDecodeResult.Error);
                    }

                    string syncSerial = protocolMessage.Action == ProtocolMessage.MessageAction.Sync
                            ? protocolMessage.ChannelSerial
                            : null;

                    channel.Presence.OnPresence(protocolMessage.Presence, syncSerial);

                    break;
            }

            return Task.FromResult(true);
        }
    }
}
