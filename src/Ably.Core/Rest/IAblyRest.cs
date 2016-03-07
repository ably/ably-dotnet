using System;
using System.Threading.Tasks;

namespace IO.Ably.Rest
{
    internal interface IAblyRest
    {
        AblyRequest CreateGetRequest(string path, ChannelOptions options = null);

        AblyRequest CreatePostRequest(string path, ChannelOptions options = null);

        Task<AblyResponse> ExecuteRequest(AblyRequest request);

        Task<T> ExecuteRequest<T>(AblyRequest request) where T : class;

        Task<DateTime> Time();
    }
}