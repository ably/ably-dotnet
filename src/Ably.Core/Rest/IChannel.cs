using System.Collections.Generic;
using System.Threading.Tasks;

namespace IO.Ably.Rest
{
    public interface IChannel
    {
        void Publish(string name, object data);
        void Publish(IEnumerable<Message> messages);

        Task<PaginatedResource<Message>> History();
        Task<PaginatedResource<Message>> History(DataRequestQuery query);
        string Name { get; }
        Task<PaginatedResource<PresenceMessage>> Presence();
        Task<PaginatedResource<PresenceMessage>> PresenceHistory();
        Task<PaginatedResource<PresenceMessage>> PresenceHistory(DataRequestQuery query);
    }
}