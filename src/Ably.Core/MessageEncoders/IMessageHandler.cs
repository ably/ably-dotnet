using System.Collections.Generic;
using Ably.Rest;

namespace Ably.MessageEncoders
{
    internal interface IMessageHandler
    {
        T ParseMessagesResponse<T>(AblyResponse response) where T : class;
        IEnumerable<Message> ParseMessagesResponse(AblyResponse response, ChannelOptions options);
    }
}