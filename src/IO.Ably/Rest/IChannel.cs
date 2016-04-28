using System.Collections.Generic;
using System.Threading.Tasks;

namespace IO.Ably.Rest
{
    public interface IChannel
    {
        Task Publish(string name, object data);
        Task Publish(Message message);
        Task Publish(IEnumerable<Message> messages);

        Task<PaginatedResult<Message>> History();
        Task<PaginatedResult<Message>> History(DataRequestQuery dataQuery);
        string Name { get; }
        
        IPresence Presence { get; }
    }

    public interface IPresence
    {
        Task<PaginatedResult<PresenceMessage>> Get(int? limit = null, string clientId = null, string connectionId = null);
        Task<PaginatedResult<PresenceMessage>> History();
        Task<PaginatedResult<PresenceMessage>> History(DataRequestQuery query);
    }
}