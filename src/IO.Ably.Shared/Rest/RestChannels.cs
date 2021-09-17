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

        private readonly List<IRestChannel> _orderedChannels = new List<IRestChannel>();
        private object _orderedListLock = new object();

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
                AddToOrderedList(result);
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
            var result = _channels.TryRemove(name, out var channel);
            RemoveFromOrderedList(channel);
            return result;
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
            lock (_orderedChannels)
            {
                return _orderedChannels.ToList().GetEnumerator();
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (_orderedChannels)
            {
                return _orderedChannels.ToList().GetEnumerator();
            }
        }

        private void AddToOrderedList(RestChannel channel)
        {
            if (_orderedChannels.Contains(channel) == false)
            {
                lock (_orderedListLock)
                {
                    if (_orderedChannels.Contains(channel) == false)
                    {
                        _orderedChannels.Add(channel);
                    }
                }
            }
        }

        private void RemoveFromOrderedList(RestChannel channel)
        {
            lock (_orderedListLock)
            {
                if (_orderedChannels.Contains(channel))
                {
                    _orderedChannels.Remove(channel);
                }
            }
        }
    }
}
