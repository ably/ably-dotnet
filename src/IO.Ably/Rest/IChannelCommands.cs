using System.Collections.Generic;

namespace IO.Ably.Rest
{
    public interface IChannelCommands : IEnumerable<IChannel>
    {
        /// <summary>
        /// Create or retrieve a channel with the specified name
        /// </summary>
        /// <param name="name">name of the channel</param>
        /// <returns>an instance of <see cref="RestChannel"/></returns>
        IChannel Get(string name);
        /// <summary>
        /// Create or retrieve a channel with the specified name and options
        /// If new options are specified the existing channel's options are updated
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

        /// <summary>
        /// Removes a specified channel from the Channels collection. 
        /// </summary>
        /// <param name="name">name of the channel</param>
        /// <returns></returns>
        bool Release(string name);
    }
}