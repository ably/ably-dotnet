﻿using Ably.Transport;
using System;
using System.Collections.Generic;

namespace Ably.Realtime
{
    public class ChannelList : IEnumerable<KeyValuePair<string, Channel>>, IChannelCommands<IRealtimeChannel>
    {
        public ChannelList(IConnectionManager connection)
        {
            this.connection = connection;
            this.channels = new Dictionary<string, Channel>();
        }
        
        private Dictionary<string, Channel> channels;
        private IConnectionManager connection;

        public IRealtimeChannel Get(string name)
        {
            Channel channel = null;
            if (!this.channels.TryGetValue(name, out channel))
            {
                channel = new Channel(name, this.connection);
                this.channels.Add(name, channel);
            }
            return channel;
        }

        public IRealtimeChannel Get(string name, ChannelOptions options)
        {
            // TODO: Implement ChannelList.Get
            throw new NotImplementedException();
        }

        public void Release(string name)
        {
            Channel channel = null;
            if (this.channels.TryGetValue(name, out channel))
            {
                channel.Detach();
                this.channels.Remove(name);
            }
        }

        public void ReleaseAll()
        {
            foreach (string channelName in this.channels.Keys)
            {
                this.Release(channelName);
            }
        }

        public IEnumerator<KeyValuePair<string, Channel>> GetEnumerator()
        {
            return this.channels.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.channels.GetEnumerator();
        }
    }
}
