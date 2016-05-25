using System;
using IO.Ably.Rest;
using System.Threading.Tasks;

namespace IO.Ably
{
    public interface IRealtimeClient : IStatsCommands
    {
        
    }

    public interface IRestClient : IStatsCommands, IChannelCommands
    {
        /// <summary>Authentication methods</summary>
        IAuthCommands Auth { get; }

        /// <summary>Channel methods</summary>
        IChannelCommands Channels { get; }

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

        /// <summary>Retrieves the stats for the application based on a custom query. It should be used with <see cref="DataRequestQuery"/>.
        /// It is mainly because of the way a PaginatedResource defines its queries. For retrieving Stats with special parameters use <see cref="AblyRest.StatsAsync(IO.Ably.StatsDataRequestQuery)"/>
        /// </summary>
        /// <example>
        /// var client = new AblyRest("validkey");
        /// var stats = client..StatsAsync();
        /// var nextPage = cliest..StatsAsync(stats.NextQuery);
        /// </example>
        /// <param name="query"><see cref="DataRequestQuery"/> and <see cref="StatsDataRequestQuery"/></param>
        /// <returns></returns>
        Task<PaginatedResult<Stats>> StatsAsync(DataRequestQuery query);
    }
}