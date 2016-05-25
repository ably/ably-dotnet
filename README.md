# ably-dotnet

[![Build Status](https://travis-ci.org/ably/ably-dotnet.svg?branch=v0.8)](https://travis-ci.org/ably/ably-dotnet)

## First BETA version it out

After a bit of restructuring and adding more test coverage we have the first BETA version of the library. There is still
work to be done. The Rest client is feature complete. The API have been made fully async. Some of the method names have changes to following the async pattern. 
The Realtime client is still missing parts of the spec. Mainly around Presence and Connection handling OS events. Other than that the rest of the realtime features are implemented and ready to use. 
The current nuget package targets only .net45 and xamarin ios and android. We are going to add more targets soon and hopefully a portable library too.

What's left

* Finish the Realtime spec implementation
* Fix the Travis build
* Add Mono support
* Allow custom json.net settings to be used when serialising the data. Even allow a custom serializer.
* Lower down the Json.Net version requirements so people are not forced to upgrade
* Test what happens when event subscribers block for a long time
* Add a list of error codes accessible to the use
* Add AppVeyor integration
* Trim the public methods to what's necessary and force comments
* Generate documentation
* Add .Net core support. 
* Add UWP apps support
* Create portable library versions

This repo contains the ably .NET client library.

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
var realtime = new AblyRealtime("<api key>");
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
var channel = realtime.Get("test");
```

Subscribe to channel events:

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
channel.PublishAsync("greeting", "Hello World!");
```

## Using the REST API

*TODO*

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

Copyright (c) 2016 Ably Real-time Ltd, Licensed under the Apache License, Version 2.0.  Refer to [LICENSE](LICENSE) for the license terms.
