using System;
using System.Threading.Tasks;
using IO.Ably.Realtime;

namespace IO.Ably
{
    public interface IRealtimeClient : IStatsCommands
    {
        void Connect();
        void Close();
        IAblyAuth Auth { get; }
        Connection Connection { get; }
        string ClientId { get; }
        RealtimeChannels Channels { get; }
        Task<DateTimeOffset> TimeAsync();
    }
}