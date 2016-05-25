using System;
using System.Collections.Generic;
using IO.Ably.Rest;

namespace IO.Ably.Realtime
{
    public interface IRealtimeChannelCommands : IEnumerable<IRealtimeChannel>
    {
        /// <summary>
        /// Create a channel with the specified name and options
        /// </summary>
        /// <param name="name">name of the channel</param>
        /// <param name="options"><see cref="ChannelOptions"/></param>
        /// <returns>an instance of <see cref="RealtimeChannel"/></returns>
        IRealtimeChannel Get(string name, ChannelOptions options = null);

        /// <summary>
        /// Same as the Get(string name)/>
        /// </summary>
        /// <param name="name">name of the channel</param>
        /// <returns>an instance of <see cref="RealtimeChannel"/></returns>
        IRealtimeChannel this[string name] { get; }

        /// <summary>
        /// Releases a channel with the specified name
        /// </summary>
        /// <param name="name">name of the channel</param>
        void Release(string name);

        /// <summary>
        /// Releases all channels
        /// </summary>
        void ReleaseAll();

        bool ContainsChannel(string name);
    }
}
