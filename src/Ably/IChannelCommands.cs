namespace Ably
{
    public interface IChannelCommands
    {
        IChannel Get(string name);
        IChannel Get(string name, ChannelOptions options);
    }
}