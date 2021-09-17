using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace IO.Ably.Rest
{
    /// <summary>
    /// Class that manages RestChannels.
    /// </summary>
    public class RestChannels : IChannels<IRestChannel>
    {
        private readonly ConcurrentDictionary<string, RestChannel> _channels =
            new ConcurrentDictionary<string, RestChannel>();

        private readonly AblyRest _ablyRest;

        internal RestChannels(AblyRest restClient)
        {
            _ablyRest = restClient;
        }

        /// <inheritdoc/>
        public IRestChannel Get(string name)
        {
            return Get(name, null);
        }

        /// <inheritdoc/>
        public IRestChannel Get(string name, ChannelOptions options)
        {
            if (!_channels.TryGetValue(name, out var result))
            {
                var channel = new RestChannel(_ablyRest, name, options);
                result = _channels.AddOrUpdate(name, channel, (s, realtimeChannel) =>
                {
                    if (options != null && realtimeChannel != null)
                    {
                        realtimeChannel.Options = options;
                    }

                    return realtimeChannel;
                });
            }
            else
            {
                if (options != null)
                {
                    result.Options = options;
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public IRestChannel this[string name] => Get(name);

        /// <inheritdoc/>
        public bool Release(string name)
        {
            return _channels.TryRemove(name, out _);
        }

        /// <inheritdoc/>
        public void ReleaseAll()
        {
            var channelList = _channels.Keys.ToArray();
            foreach (var channelName in channelList)
            {
                Release(channelName);
            }
        }

        /// <inheritdoc/>
        public bool Exists(string name)
        {
            return _channels.ContainsKey(name);
        }

        /// <inheritdoc/>
        IEnumerator<IRestChannel> IEnumerable<IRestChannel>.GetEnumerator()
        {
            return _channels.ToArray().Select(x => x.Value).GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _channels.ToArray().Select(x => x.Value).GetEnumerator();
        }
    }
}
