namespace Ably
{
    internal interface IAblyHttpClient
    {
        AblyResponse Execute(AblyRequest request);
    }
}
