# ably-dotnet

[![Build Status](https://travis-ci.org/ably/ably-dotnet.svg?branch=master)](https://travis-ci.org/ably/ably-dotnet)

A .Net client library for [ably.io](https://www.ably.io), the realtime messaging service.

For complete API documentation, see the [ably documentation](https://ably.io/documentation).

## Documentation

Visit https://www.ably.io/documentation for a complete API reference and more examples.

## Installation

The client library is available as a [nuget package](https://www.nuget.org/packages/ably.io/).

You can install it from the Package Manager Console using this command

```
PM> Install-Package ably.io
```

## Using the Realtime API

### Introduction

All examples assume a client has been created as follows:

```csharp
// using basic auth with API key
var realtime = new AblyRealtime("<api key>");
```

```csharp
// using taken auth with token string
var realtime = new AblyRealtime(new ClientOptions { Token = "token" });
```

If you do not have an API key, [sign up for a free API key now](https://www.ably.io/signup)

### Connection

Connecting and observing connection state changes. By default the library automatically initialises a connection. 

```csharp
realtime.Connection.On(ConnectionState.Connected, args =>
{
    //Do stuff  
});

```
To disable the default automatic connect behaviour of the library, set `AutoConnect=false` when initialising the client.

```csharp
var realtime = new AblyRealtime(new ClientOptions("<api key>") {AutoConnect = false});
// some code
realtime.Connect();
```

Subscribing to connection state changes and observing errors:

```csharp
realtime.Connection.On(args =>
{
    var currentState = args.Current; //Current state the connection transitioned to
    var previousState = args.Previous; // Previous state
    var error = args.Reason; // If the connection errored the Reason object will be populated.
});
```

### Subscribing to a channel

Create a channel

```csharp
var channel = realtime.Channels.Get("test");
```

Subscribing to all events:

```csharp
channel.Subscribe(message =>
{
    var name = message.name;
    var data = message.data;
});
```

Subscribing to specific events:

```csharp
channel.Subscribe("myEvent", message =>
{
    var name = message.name;
    var data = message.data;
});
```

Observing channel state changes and errors:

```csharp
channel.On(args =>
{
    var state = args.NewState; //Current channel State
    var error = args.Error; // If the channel errored it will be refrected here
});

//or

channel.On(ChannelState.Attached, args =>
{
    // Do stuff when channel is attached
});
```

### Publishing to a channel

The client support a callback and async publishing. The simplest way to publish is:

```csharp
channel.Publish("greeting", "Hello World!");
```

with a callback:

```csharp
channel.Publish("greeting", "Hello World!", (success, error) =>
{
    //if publish succeeded `success` is true
    //if publish failed `success` is false and error will contain the specific error
});
```

and the async version which if you `await` it will complete when the message has been acknowledged or rejected by the Ably service:

```csharp
var result = await channel.PublishAsync("greeting", "Hello World!");
//You can check if the message failed
if (result.IsFailure)
{
    var error = result.Error; // The error reason can be accessed as well
}
```

### Getting channel history

Calling history returns a paginated list of message. The object is of type `PaginatedResult<Message>` and can be iterated through as a normal list.  

```csharp
var history = await channel.HistoryAsync();
//loop through current history page
foreach (var message in history.Items)
{
    //Do something with message
}
//Get next page.
var nextPage = await history.NextAsync();
```

### Getting presence history

Getting presence history is similar to how message history works. You get back `PaginatedResult<PresenceMessage>` and can navigate or iterate through the page

```csharp
var presenceHistory = await channel.Presence.HistoryAsync();
//loop through the presence messages
foreach (var presence in presenceHistory.Items)
{
    //Do something with the messages
}

var presenceNextPage = await presenceHistory.NextAsync();
```

### Symmetric end-to-end encrypted payloads on a channel

When a 128 bit or 256 bit key is provided to the library, all payloads are encrypted and decrypted automatically using that key on the channel. The secret key is never transmitted to Ably and thus it is the developer's responsibility to distribute a secret key to both publishers and subscribers.

```csharp
var secret = Crypto.GetRandomKey();
var encryptedChannel = realtime.Get("encrypted", new ChannelOptions(secret));
encryptedChannel.Subscribe(message =>
{
    var data = message.data; // sensitive data (encrypted before published)
});
encryptedChannel.Publish("name (not encrypted)", "sensitive data (encrypted before published)");
```

## Using the REST API

### Introduction

The rest client provides a fully async wrapper around the Ably service web api.

All examples assume a client and/or channel has been created as follows:

```csharp
var client = new AblyRest("<api key>");
var channel = client.Channels.Get("test");
```

If you do not have an API key, [sign up for a free API key now](https://www.ably.io/signup)

### Publishing a message to a channel

```csharp
await channel.PublishAsync("name", "data");
```

If the publish is not successful an error will be thrown of type `AblyException` containing error codes and error description

```csharp
try 
{
    await channel.PublishAsync("name", "errorData");
} 
catch(AblyException ablyError) 
{
    // Log error
}
```

### Querying channel history

```csharp
var historyPage = await channel.HistoryAsync();
foreach (var message in historyPage.Items)
{
    //Do something with each message
}
//get next page
var nextHistoryPage = await historyPage.NextAsync();
```

### Current presence members on a channel

```csharp
var presence = await channel.Presence.GetAsync();
var first = presence.Items.FirstOrDefault();
var clientId = first.clientId; //clientId of the first member present
var nextPresencePage = await presence.NextAsync();
foreach (var presenceMessage in nextPresencePage.Items)
{
    //do stuff with next page presence messages
}
```

### Querying the presence history

```csharp
// Presence history
var presenceHistory = await channel.Presence.HistoryAsync();
foreach (var presenceMessage in presenceHistory.Items)
{
    // Do stuff with presence messages
}

var nextPage = await presenceHistory.NextAsync();
foreach (var presenceMessage in nextPage.Items)
{
    // Do stuff with next page messages
}
```

### Generate a Token

Tokens are issued by Ably and are readily usable by any client to connect to Ably:

```csharp
var token = await client.Auth.RequestTokenAsync();
var tokenString = token.Token; // "xVLyHw.CLchevH3hF....MDh9ZC_Q"
var tokenClient = new AblyRest(new ClientOptions { TokenDetails = token });
```
### Generate a TokenRequest

Token requests are issued by your servers and signed using your private API key. This is the preferred method of authentication as no secrets are ever shared, and the token request can be issued to trusted clients without communicating with Ably.

```csharp
var tokenRequest = await client.Auth.CreateTokenRequestAsync();
```

### Symmetric end-to-end encrypted payloads on a channel

When a 128 bit or 256 bit key is provided to the library, all payloads are encrypted and decrypted automatically using that key on the channel. The secret key is never transmitted to Ably and thus it is the developer's responsibility to distribute a secret key to both publishers and subscribers.

```csharp
var secret = Crypto.GetRandomKey();
var encryptedChannel = client.Channels.Get("encryptedChannel", new ChannelOptions(secret));
await encryptedChannel.PublishAsync("name", "sensitive data"); //Data will be encrypted before publish
var history = await encryptedChannel.HistoryAsync();
var data = history.First().data; // "sensitive data" the message will be automatically decrypted once received
```

### Fetching your application's stats

```csharp
var stats = await client.StatsAsync();
var firstItem = stats.Items.First();
var nextStatsPage = await stats.NextAsync();
```

### Fetching the Ably service time

```csharp
DateTimeOffset time = await client.TimeAsync();
```

## Dependencies

The library use the following packages:

* Newtonsoft.Json (>= 8.0.3)
* Nito.AsyncEx (>= 3.0.1)
* WebSocket4Net (>= 0.14.1)
* MsgPack.Cli (>= 0.6.8)

## Supported platforms

* Xamarin iOS and Android
* .Net 4.5+
* Mono

## Support, feedback and troubleshooting

Please visit http://support.ably.io/ for access to our knowledgebase and to ask for any assistance.

You can also view the [community reported Github issues](https://github.com/ably/ably-dotnet/issues).

## Contributing

1. Fork it
2. Create your feature branch (`git checkout -b my-new-feature`)
3. Commit your changes (`git commit -am 'Add some feature'`)
4. Ensure you have added suitable tests and the test suite is passing
4. Push to the branch (`git push origin my-new-feature`)
5. Create a new Pull Request

## License

Copyright (c) 2016 Ably Real-time Ltd, Licensed under the Apache License, Version 2.0.  Refer to [LICENSE](LICENSE) for the license terms.
