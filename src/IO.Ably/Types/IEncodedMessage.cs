using System;

namespace IO.Ably
{
    internal interface IMessage
    {
        string id { get; set; }
        object data { get; set; }
        string clientId { get; set; }
        string encoding { get; set; }
        DateTimeOffset? timestamp { get; set; }
    }
}