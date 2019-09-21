using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IO.Ably;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Types;

namespace IO.Ably.Realtime
{
    public class RealtimeChannels : IChannels<IRealtimeChannel>
    {
        internal ILogger Logger { get; private set; }

        private ConcurrentDictionary<string, RealtimeChannel> Channels { get; } = new ConcurrentDictionary<string, RealtimeChannel>();

        private readonly AblyRealtime _realtimeClient;

        internal RealtimeChannels(AblyRealtime realtimeClient)
        {
            Logger = realtimeClient.Logger;
            _realtimeClient = realtimeClient;
        }

        public IRealtimeChannel Get(string name)
        {
            return Get(name, null);
        }



        public IRealtimeChannel Get(string name, ChannelOptions options)
        {
            // if the channel cannot be found
            if (!Channels.TryGetValue(name, out var result))
            {
                // create a new instance using the passed in option
                var channel = new RealtimeChannel(name, _realtimeClient.Options.GetClientId(), _realtimeClient, options);
                result = Channels.AddOrUpdate(name, channel, (s, realtimeChannel) =>
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
                {
                    result.Options = options;
                }
            }

            return result;
        }

        public IRealtimeChannel this[string name] => Get(name);

        public bool Release(string name)
        {
            if (Logger.IsDebug) { Logger.Debug($"Releasing channel #{name}"); }
            RealtimeChannel channel = null;
            if (Channels.TryGetValue(name, out channel))
            {
                EventHandler<ChannelStateChange> eventHandler = null;
                eventHandler = (s, args) =>
                {
                    var detachedChannel = (RealtimeChannel)s;
                    if (args.Current == ChannelState.Detached || args.Current == ChannelState.Failed)
                    {
                        if (Logger.IsDebug) { Logger.Debug($"Channel #{name} was removed from Channel list. State {args.Current}"); }
                        detachedChannel.InternalStateChanged -= eventHandler;

                        RealtimeChannel removedChannel;
                        if (Channels.TryRemove(name, out removedChannel))
                        {
                            removedChannel.Dispose();
                        }
                    }
                    else
                    {
                        if (Logger.IsDebug) { Logger.Debug($"Waiting to remove Channel #{name}. State {args.Current}"); }
                    }
                };

                channel.InternalStateChanged += eventHandler;
                channel.Detach();
                return true;
            }

            return false;
        }

        public void ReleaseAll()
        {
            var channelList = Channels.Keys.ToArray();
            foreach (var channelName in channelList)
            {
                Release(channelName);
            }
        }

        public bool Exists(string name)
        {
            return Channels.ContainsKey(name);
        }

        IEnumerator<IRealtimeChannel> IEnumerable<IRealtimeChannel>.GetEnumerator()
        {
            return Channels.ToArray().Select(x => x.Value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Channels.ToArray().Select(x => x.Value).GetEnumerator();
        }
    }
}
