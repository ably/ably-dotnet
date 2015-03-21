using System;
using System.Collections.Generic;

namespace Ably.Realtime
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(IEnumerable<Message> messages)
        {
            this.Messages = messages;
        }

        public IEnumerable<Message> Messages { get; private set; }
    }
}
