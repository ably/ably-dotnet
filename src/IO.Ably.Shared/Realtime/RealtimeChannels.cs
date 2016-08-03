using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using IO.Ably.Rest;

namespace IO.Ably.Realtime
{
    public class RealtimeChannels : IChannels<IRealtimeChannel>
    {
        private ConcurrentDictionary<string, RealtimeChannel> _channels { get; } = new ConcurrentDictionary<string, RealtimeChannel>();

        private readonly AblyRealtime _realtimeClient;

        internal RealtimeChannels(AblyRealtime realtimeClient)
        {
            _realtimeClient = realtimeClient;
        }

        public IRealtimeChannel Get(string name)
        {
            return Get(name, null);
        }

        public IRealtimeChannel Get(string name, ChannelOptions options)
        {
            RealtimeChannel result = null;
            if (!_channels.TryGetValue(name, out result))
            {
                var channel = new RealtimeChannel(name, _realtimeClient.Options.GetClientId(), _realtimeClient, options);
                result = _channels.AddOrUpdate(name, channel, (s, realtimeChannel) =>
                {
                    if (options != null)
                    {
                        realtimeChannel.Options = options;
                    }
                    return realtimeChannel;
                });
            }
            else
            {
                if (options != null)
                    result.Options = options;
            }
            return result;
        }

        public IRealtimeChannel this[string name] => Get(name);

        public bool Release(string name)
        {
            if(Logger.IsDebug) {  Logger.Debug($"Releasing channel #{name}"); }
            RealtimeChannel channel = null;
            if (_channels.TryGetValue(name, out channel))
            {
                EventHandler<ChannelStateChange> eventHandler = null;
                eventHandler = (s, args) =>
                {
                    var detachedChannel = (RealtimeChannel) s;
                    if (args.Current == ChannelState.Detached || args.Current == ChannelState.Failed)
                    {
                        if (Logger.IsDebug) { Logger.Debug($"Channel #{name} was removed from Channel list. State {args.Current}"); }
                        detachedChannel.StateChanged -= eventHandler;

                        RealtimeChannel removedChannel;
                        if (_channels.TryRemove(name, out removedChannel))
                            removedChannel.Dispose();
                    }
                    else
                    {
                        if (Logger.IsDebug) { Logger.Debug($"Waiting to remove Channel #{name}. State {args.Current}"); }
                    }
                };
                channel.StateChanged += eventHandler;
                channel.Detach();
                return true;
            }
            return false;
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

        IEnumerator<IRealtimeChannel> IEnumerable<IRealtimeChannel>.GetEnumerator()
        {
            return _channels.ToArray().Select(x => x.Value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _channels.ToArray().Select(x => x.Value).GetEnumerator();
        }
    }
}