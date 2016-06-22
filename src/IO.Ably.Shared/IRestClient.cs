using System;
using IO.Ably.Rest;
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
        IRealtimeChannels Channels { get; }
        Task<DateTimeOffset> TimeAsync();
    }

    public interface IRestClient : IStatsCommands, IRestChannels
    {
        /// <summary>Authentication methods</summary>
        IAblyAuth Auth { get; }

        /// <summary>Channel methods</summary>
        IRestChannels Channels { get; }

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