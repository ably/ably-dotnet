# ably-dotnet

[![Build Status](https://travis-ci.org/ably/ably-dotnet.svg?branch=master)](https://travis-ci.org/ably/ably-dotnet)

This repo contains the [Ably](https://www.ably.io) .NET client library.

For complete API documentation, see the [Ably documentation](https://www.ably.io/documentation).

## Warning: Pre-release status

This client library's API is subject to change significantly in April 2016. We believe that the current API is unsuitalbe for the modern .NET ecosystem (specifically Xamarin, Mono, and Windows Mobile) and as such we are updating the entire API to use the [async/await Task API for asynchronous .NET APIs](https://msdn.microsoft.com/en-us/library/hh191443.aspx). If you plan on using this client library, please expect a significant breaking change in the next release. [Contact our tech team](https://www.ably.io/contact) if you would like to get an update on the status, would perhaps like to use our WIP API, or have any questions. 

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
var realtime = new Ably.Realtime("<api key>");
```

### Connection

Connecting and observing connection state changes

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

## Support, feedback and troubleshooting

Please visit http://support.ably.io/ for access to our knowledgebase and to ask for any assistance.

You can also view the [community reported Github issues](https://github.com/ably/ably-dotnet/issues).

To see what has changed in recent versions of Bundler, see the [CHANGELOG](CHANGELOG.md).

## Contributing

1. Fork it
2. Create your feature branch (`git checkout -b my-new-feature`)
3. Commit your changes (`git commit -am 'Add some feature'`)
4. Ensure you have added suitable tests and the test suite is passing
4. Push to the branch (`git push origin my-new-feature`)
5. Create a new Pull Request

## License

Copyright (c) 2015 Ably Real-time Ltd, Licensed under the Apache License, Version 2.0.  Refer to [LICENSE](LICENSE) for the license terms.
