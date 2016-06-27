using System;
using IO.Ably.Rest;
using System.Threading.Tasks;

namespace IO.Ably
{
    public interface IRestClient : IStatsCommands
    {
        /// <summary>Authentication methods</summary>
        IAblyAuth Auth { get; }

        /// <summary>Channel methods</summary>
        RestChannels Channels { get; }

        /// <summary>Retrieves the ably service time</summary>
        /// <returns></returns>
        Task<DateTimeOffset> TimeAsync();
    }

    public interface IStatsCommands
    {
        /// <summary>Retrieves the stats for the application. Passed default <see cref="StatsDataRequestQuery"/> for the request</summary>
        /// <returns></returns>
        Task<PaginatedResult<Stats>> StatsAsync();

        /// <summary>Retrieves the stats for the application using a more specific stats query. Check <see cref="StatsDataRequestQuery"/> for more information</summary>
        /// <param name="query">stats query</param>
        /// <returns></returns>
        Task<PaginatedResult<Stats>> StatsAsync(StatsDataRequestQuery query);
    }
}