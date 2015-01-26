using System.Collections.Generic;

namespace Ably
{
    internal interface IResponseHandler
    {
        T ParseMessagesResponse<T>(AblyResponse response) where T : class;
        IEnumerable<Message> ParseMessagesResponse(AblyResponse response, ChannelOptions options);
    }
}