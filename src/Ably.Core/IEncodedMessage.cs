namespace Ably
{
    internal interface IEncodedMessage
    {
        object Data { get; set; }
        string Encoding { get; set; }
    }
}