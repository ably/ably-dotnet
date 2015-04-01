using System;
using System.Collections.Generic;

namespace Ably.Realtime
{
    public interface IRealtimeChannelCommands<TChannel> : Rest.IChannelCommands<TChannel>, IEnumerable<TChannel>
        where TChannel : IRealtimeChannel
    {
        void Release(string name);
        void ReleaseAll();
    }
}
