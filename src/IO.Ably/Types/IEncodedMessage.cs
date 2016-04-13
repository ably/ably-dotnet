namespace IO.Ably
{
    internal interface IEncodedMessage
    {
        object data { get; set; }
        string encoding { get; set; }
    }
}