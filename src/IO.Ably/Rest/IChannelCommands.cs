using System.Collections.Generic;

namespace IO.Ably.Rest
{
    public interface IChannelCommands : IEnumerable<IChannel>
    {
        /// <summary>
        /// Create a channel with the specified name
        /// </summary>
        /// <param name="name">name of the channel</param>
        /// <returns>an instance of <see cref="RestChannel"/></returns>
        IChannel Get(string name);
        /// <summary>
        /// Create a channel with the specified name and options
        /// </summary>
        /// <param name="name">name of the channel</param>
        /// <param name="options"><see cref="ChannelOptions"/></param>
        /// <returns>an instance of <see cref="RestChannel"/></returns>
        IChannel Get(string name, ChannelOptions options);
        /// <summary>
        /// Same as the Get(string name)/>
        /// </summary>
        /// <param name="name">name of the channel</param>
        /// <returns>an instance of <see cref="RestChannel"/></returns>
        IChannel this[string name] { get; }
    }
}