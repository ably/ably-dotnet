using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using IO.Ably.Realtime;
using IO.Ably.Rest;

namespace IO.Ably
{
    public interface IChannels<out T> : IEnumerable<T>
    {
        /// <summary>
        /// Create or retrieve a channel with the specified name
        /// </summary>
        /// <param name="name">name of the channel</param>
        /// <returns>an instance of <see cref="RestChannel"/></returns>
        T Get(string name);
        /// <summary>
        /// Create or retrieve a channel with the specified name and options
        /// If new options are specified the existing channel's options are updated
        /// </summary>
        /// <param name="name">name of the channel</param>
        /// <param name="options"><see cref="ChannelOptions"/></param>
        /// <returns>an instance of <see cref="RestChannel"/></returns>
        T Get(string name, ChannelOptions options);
        /// <summary>
        /// Same as the Get(string name)/>
        /// </summary>
        /// <param name="name">name of the channel</param>
        /// <returns>an instance of <see cref="RestChannel"/></returns>
        T this[string name] { get; }

        /// <summary>
        /// Removes a specified channel from the Channels collection. 
        /// </summary>
        /// <param name="name">name of the channel</param>
        /// <returns></returns>
        bool Release(string name);

        void ReleaseAll();

        bool Exists(string name);
    }
}