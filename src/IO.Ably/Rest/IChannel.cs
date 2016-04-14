using System.Collections.Generic;
using System.Threading.Tasks;

namespace IO.Ably.Rest
{
    public interface IChannel
    {
        Task Publish(string name, object data);
        Task Publish(Message message);
        Task Publish(IEnumerable<Message> messages);

        Task<PaginatedResource<Message>> History();
        Task<PaginatedResource<Message>> History(DataRequestQuery query);
        string Name { get; }
        Task<PaginatedResource<PresenceMessage>> Presence();
        Task<PaginatedResource<PresenceMessage>> PresenceHistory();
        Task<PaginatedResource<PresenceMessage>> PresenceHistory(DataRequestQuery query);
    }
}