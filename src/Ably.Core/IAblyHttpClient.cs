namespace IO.Ably
{
    internal interface IAblyHttpClient
    {
        AblyResponse Execute(AblyRequest request);
    }
}
