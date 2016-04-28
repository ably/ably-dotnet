﻿using IO.Ably.Transport;

namespace IO.Ably.Realtime
{
    public interface IChannelFactory
    {
        IRealtimeChannel Create( string channelName );
    }

    public class ChannelFactory : IChannelFactory
    {
        internal IConnectionManager ConnectionManager { get; set; }
        public ClientOptions Options { get; set; }

        public IRealtimeChannel Create( string channelName )
        {
            return new RealtimeChannel( channelName, this.Options.ClientId, this.ConnectionManager );
        }
    }
}