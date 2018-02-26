using System;

namespace IO.Ably
{
    internal interface IMessage
    {
        string Id { get; set; }

        string ConnectionId { get; set; }

        object Data { get; set; }

        string ClientId { get; set; }

        string Encoding { get; set; }

        DateTimeOffset? Timestamp { get; set; }
    }
}
