using System.Collections.Generic;
using System.Threading.Tasks;

namespace IO.Ably.Rest
{
    public interface IRestChannel
    {
        Task PublishAsync(string name, object data, string clientId = null);
        Task PublishAsync(Message message);
        Task PublishAsync(IEnumerable<Message> messages);

        Task<PaginatedResult<Message>> HistoryAsync();
        Task<PaginatedResult<Message>> HistoryAsync(HistoryRequestParams query);
        string Name { get; }

        IPresence Presence { get; }

        void Publish(string name, object data, string clientId = null);
        void Publish(Message message);
        void Publish(IEnumerable<Message> messages);
        PaginatedResult<Message> History();
        PaginatedResult<Message> History(HistoryRequestParams query);
    }

    public interface IPresence
    {
        Task<PaginatedResult<PresenceMessage>> GetAsync(int? limit = null, string clientId = null, string connectionId = null);
        Task<PaginatedResult<PresenceMessage>> HistoryAsync();
        Task<PaginatedResult<PresenceMessage>> HistoryAsync(HistoryRequestParams query);
        Task<PaginatedResult<PresenceMessage>> GetAsync(HistoryRequestParams query);
    }
}