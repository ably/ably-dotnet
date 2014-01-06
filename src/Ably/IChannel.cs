using System.Collections.Generic;

namespace Ably
{
    public interface IChannel
    {
        void Publish(string name, object data);
        void Publish(IEnumerable<Message> messages);
        IList<Message> History();
        IList<Message> History(HistoryDataRequestQuery query);
        IList<Stats> Stats();
        IList<Stats> Stats(DataRequestQuery query);
        string Name { get; }
        IList<PresenceMessage> Presence();


    }
}