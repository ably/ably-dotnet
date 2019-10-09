namespace IO.Ably.Realtime.Workflow
{
    internal class ChannelCommand : RealtimeCommand
    {
        public string ChannelName { get; set; }

        public RealtimeCommand Command { get; set; }

        public ChannelCommand(string channelName, RealtimeCommand command)
        {
            ChannelName = channelName;
            Command = command;
        }

        public static ChannelCommand Create(string channelName, RealtimeCommand command) =>
            new ChannelCommand(channelName, command);

        public static ChannelCommand CreateForAllChannels(RealtimeCommand command) =>
            new ChannelCommand(null, command);

        protected override string ExplainData()
        {
            return $"ChannelName: {ChannelName}, Command: {Command.Name}";
        }
    }

    internal class InitialiseFailedChannelsOnConnect : RealtimeCommand
    {
        public static InitialiseFailedChannelsOnConnect Create() => new InitialiseFailedChannelsOnConnect();

        protected override string ExplainData()
        {
            return string.Empty;
        }
    }
}
