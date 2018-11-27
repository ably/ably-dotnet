# ably-dotnet

A .NET client library for [www.ably.io](https://www.ably.io), the realtime messaging service.

## Supported platforms

* .NET 4.6+ &ast;
* .NET Core &ast;&ast;
* .NET Standard 1.4+
* Mono 5.4+
* UWP
* [Xamarin.Android 8.0+](https://developer.xamarin.com/releases/android/xamarin.android_8/xamarin.android_8.0/)
* [Xamarin.iOS 11.4+](https://developer.xamarin.com/releases/ios/xamarin.ios_11/xamarin.ios_11.4/)

Unity support is currently in beta. See below for details on why it's considered beta.

Unity Requirements:

- Unity 2018.2.0 or newer
- The following Unity Player settings must be applied:
  - Scripting Runtime Version should be '.NET 4.x Equivelant'
  - Api Compatibility Level should be '.NET Standard 2.0'
- Json.NET 9.0.1 or newer. If you are targetting platforms that require the IL2CPP scripting backend then a version of Json.NET that has been modified to work with the Unity IL2CPP AOT compiler is required, we have had success with [Json.Net.Unity3D](https://github.com/SaladLab/Json.Net.Unity3D)

The .NET Standard build of ably-dotnet (IO.Ably.dll) needs to be added the asset folder of your Unity project.
As Unity does not support Nuget out of the box we currently recommend building ably-dotnet from source, although it should be possible to [extract the required assembly from the nuget package](https://articles.runtings.co.uk/2014/09/easily-extracting-nupkg-files-with.html) or use a [3rd party Nuget extension for Unity](https://assetstore.unity.com/packages/tools/utilities/nuget-for-unity-104640), but those options are beyond the scope of this document. To build from source clone this repository and build the IO.Ably.NETStandard20 project, this can be done from Visual Studio by opening the IO.Ably.sln file or via the command line. To build via the comandline `cd` to `ably-dotnet/src/IO.Ably.NETStandard20/` and run `dotnet build`, the build output can then be found in `ably-dotnet/src/IO.Ably.NETStandard20/bin/Release/netstandard2.0`, navigate there to obtain the required `IO.Ably.dll`.
Finally, install a compatible version of Json.NET into your Unity projects asset folder (e.g. [Json.Net.Unity3D](https://github.com/SaladLab/Json.Net.Unity3D)).

A portable class library (PCL) version is not available. See [this comment](https://github.com/ably/ably-dotnet/issues/182#issuecomment-366939087) for more information on this choice and the potential workarounds that are available. 

&ast; To target Windows 7 (with .Net 4.6) a custom [ITransportFactory](https://github.com/ably/ably-dotnet/blob/master/src/IO.Ably.Shared/Transport/ITransport.cs) will need to be implemented in your project that uses an alternate Web Socket library. 
This is because [System.Net.WebSockets]('https://msdn.microsoft.com/en-us/library/system.net.websockets(v=vs.110).aspx') is not fully implementented on Windows 7.
See [this repository](https://github.com/ably-forks/ably-dotnet-alternative-transports) for a working example using the [websocket4net library](https://github.com/kerryjiang/WebSocket4Net).

&ast;&ast; We regression-test the library against .NET Core 2 but it is designed to be compatible with all versions of .NET Core (and any other runtime implementation that is compatible with .NET Standard 1.4 or greater). If you find any compatibility issues, please do [raise an issue](https://github.com/ably/ably-dotnet/issues) in this repository or contact Ably customer support for advice. Any known runtime incompatibilities can be found [here](https://github.com/ably/ably-dotnet/issues?q=is%3Aissue+is%3Aopen+label%3A%22compatibility%22).

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
    var name = message.Name;
    var data = message.Data;
});
```

Subscribing to specific events:

```csharp
channel.Subscribe("myEvent", message =>
{
    var name = message.Name;
    var data = message.Data;
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

### Using the AuthCallback

A callback to obtain a signed `TokenRequest` string or a `TokenDetails` instance.

To use `AuthCallback` create a `ClientOptions` instance and assign an appropriate delegate to the `AuthCallback` property and pass the `ClientOptions` to a new `AblyRealtime` instance.

```csharp
var options = new ClientOptions
{
    AuthCallback = async tokenParams =>
    {
        // Return a TokenDetails instance or a preferably a TokenRequest string.
        // Typically this method would wrap a request to your web server.
        return await GetTokenDetailsOrTokenRequestStringFromYourServer();        
    }
};
var client = new AblyRealtime(options);
```

### Generate a TokenRequest

Token requests are issued by your servers and signed using your private API key. This is the preferred method of authentication as no secrets are ever shared, and the token request can be issued to trusted clients without communicating with Ably.

```csharp
string tokenRequest = await client.Auth.CreateTokenRequestAsync();
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

This library has dependencies that can differ depending on the target platform.
See [the nuget page](http://nuget.org/packages/ably.io/) for specifics.

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

## Building and Packaging

The build scripts are written in powershell using PSake and need to be run on Windows with Visual Studio 2017 installed. Additionally nuget.exe and GitVersion.exe are required, these can be installed via [chocolatey](https://chocolatey.org)

    choco install nuget.commandline
    choco install gitversion.portable

Running `.\build.ps1` will start the build process and run the tests. 
Running `package.ps1` will run the build script and create a nuget package.

## Release process

This library uses [semantic versioning](http://semver.org/). For each release, the following needs to be done:

* Update the version number in [GitVersion.yml](./GetVersion.yml)&dagger; and commit the change.
* Run [`github_changelog_generator`](https://github.com/skywinder/Github-Changelog-Generator) to automate the update of the [CHANGELOG](./CHANGELOG.md). Once the `CHANGELOG` update has completed, manually change the `Unreleased` heading and link with the current version number such as `v1.0.0`. Also ensure that the `Full Changelog` link points to the new version tag instead of the `HEAD`. Commit this change.
* Add a tag for the version and push to origin such as `git tag 1.0.0 && git push origin 1.0.0`. For beta versions the version string should be `Maj.Min.Patch-betaN`, e.g `1.0.0-beta1`
* Visit [https://github.com/ably/ably-dotnet/tags](https://github.com/ably/ably-dotnet/tags) and `Add release notes` for the release including links to the changelog entry.
* Run `package.ps1` to create the nuget package. 
* Run `nuget push ably.io.*.nupkg -Source https://www.nuget.org/api/v2/package` (a private nuget API Key is required to complete this step, more information on publishing nuget packages can be found [here](https://docs.microsoft.com/en-us/nuget/quickstart/create-and-publish-a-package))

&dagger; GitVersion is required, see the preceeding section 'Building and Packaging' for more information.

## License

Copyright (c) 2016 Ably Real-time Ltd, Licensed under the Apache License, Version 2.0.  Refer to [LICENSE](LICENSE) for the license terms.
