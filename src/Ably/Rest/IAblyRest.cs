using System;

namespace Ably.Rest
{
    internal interface IAblyRest
    {
        AblyRequest CreateGetRequest(string path, ChannelOptions options = null);

        AblyRequest CreatePostRequest(string path, ChannelOptions options = null);

        AblyResponse ExecuteRequest(AblyRequest request);

        T ExecuteRequest<T>(AblyRequest request) where T : class;

        DateTimeOffset Time();
    }
}
