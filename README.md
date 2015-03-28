# ably-dotnet

This repo contains the ably .NET client library.

For complete API documentation, see the [ably documentation](https://ably.io/documentation).

## Using the Realtime API
### Introduction

All examples assume a client has been created as follows:

```csharp
var realtime = new Ably.Realtime("<api key>");
```

### Connection

Connecting and observing connection status

```csharp
realtime.Connection.ConnectionStateChanged += (s, args) =>
{
    if (args.CurrentState == ConnectionState.Connected)
    {
        // Do stuff
    }
};
realtime.Connect();
```

### Subscribing to a channel

Given:

```csharp
var channel = realtime.Channels.Get("test");
```

Subscribe to all events:

```csharp
channel.ChannelStateChanged += (s, args) =>
{
    if (args.NewState == ChannelState.Attached)
    {
        // Do stuff
    }
};
```

### Publishing to a channel

```csharp
channel.Publish("greeting", "Hello World!");
```

## Using the REST API
