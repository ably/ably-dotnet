using System;
using System.Threading.Tasks;

using IO.Ably.Rest;

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

        DateTimeOffset Time();
    }

    public interface IStatsCommands
    {
        /// <summary>Retrieves the stats for the application. Passed default <see cref="StatsRequestParams"/> for the request</summary>
        /// <returns></returns>
        Task<PaginatedResult<Stats>> StatsAsync();

        /// <summary>Retrieves the stats for the application using a more specific stats query. Check <see cref="StatsRequestParams"/> for more information</summary>
        /// <param name="query">stats query</param>
        /// <returns></returns>
        Task<PaginatedResult<Stats>> StatsAsync(StatsRequestParams query);

        PaginatedResult<Stats> Stats();
        PaginatedResult<Stats> Stats(StatsRequestParams query);
    }
}