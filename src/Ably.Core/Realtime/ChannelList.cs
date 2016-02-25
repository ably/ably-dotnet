using System;
using System.Collections.Generic;
using System.Linq;
using IO.Ably.Transport;

namespace IO.Ably.Realtime
{
    public class ChannelList : IRealtimeChannelCommands
    {
        internal ChannelList(IConnectionManager connection, IChannelFactory factory)
        {
            this.connection = connection;
            this.channels = new Dictionary<string, IRealtimeChannel>();
            this.channelFactory = factory;
        }

        private Dictionary<string, IRealtimeChannel> channels;
        private IConnectionManager connection;
        private IChannelFactory channelFactory;

        public IRealtimeChannel this[string name]
        {
            get { return this.Get(name); }
        }

        public IRealtimeChannel Get(string name)
        {
            IRealtimeChannel channel = null;
            if (!this.channels.TryGetValue(name, out channel))
            {
                channel = this.channelFactory.Create(name);
                this.channels.Add(name, channel);
            }
            return channel;
        }

        public IRealtimeChannel Get(string name, Rest.ChannelOptions options)
        {
            IRealtimeChannel channel = this.Get(name);
            channel.Options = options;
            return channel;
        }

        public void Release(string name)
        {
            IRealtimeChannel channel = null;
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
