using Ably.Transport;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ably.Realtime
{
    public class ChannelList : IRealtimeChannelCommands
    {
        internal ChannelList(IConnectionManager connection, IPresenceFactory factory)
        {
            this.connection = connection;
            this.channels = new Dictionary<string, Channel>();
            this.presenceFactory = factory;
        }
        
        private Dictionary<string, Channel> channels;
        private IConnectionManager connection;
        private IPresenceFactory presenceFactory;

        public IRealtimeChannel this[string name]
        {
            get { return this.Get(name); }
        }

        public IRealtimeChannel Get(string name)
        {
            Channel channel = null;
            if (!this.channels.TryGetValue(name, out channel))
            {
                channel = new Channel(name, this.connection, this.presenceFactory);
                this.channels.Add(name, channel);
            }
            return channel;
        }

        public IRealtimeChannel Get(string name, Rest.ChannelOptions options)
        {
            Channel channel = this.Get(name) as Channel;
            channel.Options = options;
            return channel;
        }

        public void Release(string name)
        {
            Channel channel = null;
            if (this.channels.TryGetValue(name, out channel))
            {
                EventHandler<ChannelStateChangedEventArgs> eventHandler = null;
                eventHandler = (s, args) =>
                {
                    if (args.NewState == ChannelState.Detached || args.NewState == ChannelState.Failed)
                    {
                        channel.ChannelStateChanged -= eventHandler;
                        this.channels.Remove(name);
                    }
                };
                channel.ChannelStateChanged += eventHandler;
                channel.Detach();
            }
        }

        public void ReleaseAll()
        {
            string[] channelList = this.channels.Keys.ToArray();
            foreach (string channelName in channelList)
            {
                this.Release(channelName);
            }
        }

        public IEnumerator<IRealtimeChannel> GetEnumerator()
        {
            return this.channels.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
