﻿using System.Collections.Generic;

namespace Ably
{
    public interface IChannel
    {
        void Publish(string name, object data);
        void Publish(IEnumerable<Message> messages);
        IPartialResult<Message> History();
        IPartialResult<Message> History(HistoryDataRequestQuery query);
        string Name { get; }
        IList<PresenceMessage> Presence();
    }
}