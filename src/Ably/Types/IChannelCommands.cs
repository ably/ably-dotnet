namespace Ably
{
    public interface IChannelCommands<TChannel>
        where TChannel : IChannel
    {
        TChannel Get(string name);
        TChannel Get(string name, ChannelOptions options);
    }
}