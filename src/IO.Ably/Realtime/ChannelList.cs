using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IO.Ably.Rest;
using IO.Ably.Transport;

namespace IO.Ably.Realtime
{
    public class ChannelList : IRealtimeChannelCommands
    {
        private readonly IChannelFactory channelFactory;

        private readonly Dictionary<string, IRealtimeChannel> channels;

        internal ChannelList(IChannelFactory factory)
        {
            channels = new Dictionary<string, IRealtimeChannel>();
            channelFactory = factory;
        }

        public IRealtimeChannel this[string name]
        {
            get { return Get(name); }
        }

        public IRealtimeChannel Get(string name)
        {
            IRealtimeChannel channel = null;
            if (!channels.TryGetValue(name, out channel))
            {
                channel = channelFactory.Create(name);
                channels.Add(name, channel);
            }
            return channel;
        }

        public IRealtimeChannel Get(string name, ChannelOptions options)
        {
            var channel = Get(name);
            channel.Options = options;
            return channel;
        }

        public void Release(string name)
        {
            IRealtimeChannel channel = null;
            if (channels.TryGetValue(name, out channel))
            {
                EventHandler<ChannelStateChangedEventArgs> eventHandler = null;
                eventHandler = (s, args) =>
                {
                    if (args.NewState == ChannelState.Detached || args.NewState == ChannelState.Failed)
                    {
                        channel.ChannelStateChanged -= eventHandler;
                        channels.Remove(name);
                    }
                };
                channel.ChannelStateChanged += eventHandler;
                channel.Detach();
            }
        }

        public void ReleaseAll()
        {
            var channelList = channels.Keys.ToArray();
            foreach (var channelName in channelList)
            {
                Release(channelName);
            }
        }

        public IEnumerator<IRealtimeChannel> GetEnumerator()
        {
            return channels.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}