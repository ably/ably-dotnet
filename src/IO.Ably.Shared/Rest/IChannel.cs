using System.Collections.Generic;
using System.Threading.Tasks;

namespace IO.Ably.Rest
{
    public interface IChannel
    {
        Task PublishAsync(string name, object data, string clientId = null);
        Task PublishAsync(Message message);
        Task PublishAsync(IEnumerable<Message> messages);

        Task<PaginatedResult<Message>> HistoryAsync();
        Task<PaginatedResult<Message>> HistoryAsync(DataRequestQuery dataQuery);
        string Name { get; }
        
        IPresence Presence { get; }
    }

    public interface IPresence
    {
        Task<PaginatedResult<PresenceMessage>> GetAsync(int? limit = null, string clientId = null, string connectionId = null);
        Task<PaginatedResult<PresenceMessage>> HistoryAsync();
        Task<PaginatedResult<PresenceMessage>> HistoryAsync(DataRequestQuery query);
        Task<PaginatedResult<PresenceMessage>> GetAsync(DataRequestQuery query);
    }
}