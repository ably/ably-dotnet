using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace IO.Ably.Rest
{
    public class RestChannels : IChannels<IRestChannel>
    {
        private readonly ConcurrentDictionary<string, RestChannel> _channels = new ConcurrentDictionary<string, RestChannel>();

        private readonly AblyRest _ablyRest;

        internal RestChannels(AblyRest restClient)
        {
            _ablyRest = restClient;
        }

        public IRestChannel Get(string name)
        {
            return Get(name, null);
        }

        public IRestChannel Get(string name, ChannelOptions options)
        {
            if (!_channels.TryGetValue(name, out var result))
            {
                var channel = new RestChannel(_ablyRest, name, options);
                result = _channels.AddOrUpdate(name, channel, (s, realtimeChannel) =>
                {
                    if (options != null)
                    {
                        if (result != null)
                        {
                            result.Options = options;
                        }
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

        public IRestChannel this[string name] => Get(name);

        public bool Release(string name)
        {
            return _channels.TryRemove(name, out _);
        }

        public void ReleaseAll()
        {
            var channelList = _channels.Keys.ToArray();
            foreach (var channelName in channelList)
            {
                Release(channelName);
            }
        }

        public bool Exists(string name)
        {
            return _channels.ContainsKey(name);
        }

        IEnumerator<IRestChannel> IEnumerable<IRestChannel>.GetEnumerator()
        {
            return _channels.ToArray().Select(x => x.Value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _channels.ToArray().Select(x => x.Value).GetEnumerator();
        }
    }
}
