using System.Collections.Generic;

namespace Ably
{
    public interface IChannel
    {
        void Publish(string name, object data);
        void Publish(IEnumerable<Message> messages);
        IPaginatedResource<Message> History();
        IPaginatedResource<Message> History(DataRequestQuery query);
        string Name { get; }
        IPaginatedResource<PresenceMessage> Presence();
        IPaginatedResource<PresenceMessage> PresenceHistory();
        IPaginatedResource<PresenceMessage> PresenceHistory(DataRequestQuery query);
    }
}