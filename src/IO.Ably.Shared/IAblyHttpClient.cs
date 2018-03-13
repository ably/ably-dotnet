using System.Threading.Tasks;

namespace IO.Ably
{
    internal interface IAblyHttpClient
    {
        Task<AblyResponse> Execute(AblyRequest request);
    }
}
