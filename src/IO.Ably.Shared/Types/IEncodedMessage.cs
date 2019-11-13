using System;

namespace IO.Ably
{
    // No need to document an internal interface.
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1649 // File name should match first type name

    internal interface IPayload
    {
        object Data { get; set; }

        string Encoding { get; set; }
    }

    internal interface IMessage : IPayload
    {
        string Id { get; set; }

        string ConnectionId { get; set; }

        object Data { get; set; }

        string ClientId { get; set; }

        string Encoding { get; set; }

        DateTimeOffset? Timestamp { get; set; }
    }
#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1600 // Elements should be documented

}
