namespace Ably
{
    public interface IAblyHttpClient
    {
        AblyResponse Execute(AblyRequest request);
    }
}
