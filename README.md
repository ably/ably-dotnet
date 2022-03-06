# ably-dotnet

[![NuGet version](https://badge.fury.io/nu/ably.io.svg)](https://badge.fury.io/nu/ably.io)
[![Windows - build and test](https://github.com/ably/ably-dotnet/actions/workflows/build-and-test-windows.yml/badge.svg)](https://github.com/ably/ably-dotnet/actions/workflows/build-and-test-windows.yml)
[![MacOS - build and test](https://github.com/ably/ably-dotnet/actions/workflows/build-and-test-macos.yml/badge.svg)](https://github.com/ably/ably-dotnet/actions/workflows/build-and-test-macos.yml)
[![Linux - build and test](https://github.com/ably/ably-dotnet/actions/workflows/build-and-test-linux.yml/badge.svg)](https://github.com/ably/ably-dotnet/actions/workflows/build-and-test-linux.yml)

_[Ably](https://ably.com) is the platform that powers synchronized digital experiences in realtime. Whether attending an event in a virtual venue, receiving realtime financial information, or monitoring live car performance data – consumers simply expect realtime digital experiences as standard. Ably provides a suite of APIs to build, extend, and deliver powerful digital experiences in realtime for more than 250 million devices across 80 countries each month. Organizations like Bloomberg, HubSpot, Verizon, and Hopin depend on Ably’s platform to offload the growing complexity of business-critical realtime data synchronization at global scale. For more information, see the [Ably documentation](https://ably.com/documentation)._

This is a .NET client library for Ably. The library currently targets the [Ably 1.1-beta client library specification](https://www.ably.io/documentation/client-lib-development-guide/features/). You can jump to the '[Known Limitations](#known-limitations)' section to see the features this client library does not yet support or or [view our client library SDKs feature support matrix](https://www.ably.io/download/sdk-feature-support-matrix) to see the list of all the available features.

## Supported platforms

* .NET 4.6.2+ &ast;
* .NET Core &ast;&ast;
* .NET Standard 2.0+
* Mono 5.4+
* [Xamarin.Android 8.0+](https://developer.xamarin.com/releases/android/xamarin.android_8/xamarin.android_8.0/)
* [Xamarin.iOS 11.4+](https://developer.xamarin.com/releases/ios/xamarin.ios_11/xamarin.ios_11.4/)

&ast; To target Windows 7 (with .NET 4.6) a custom [ITransportFactory](https://github.com/ably/ably-dotnet/blob/main/src/IO.Ably.Shared/Transport/ITransport.cs) will need to be implemented in your project that uses an alternate Web Socket library. 
This is because [`System.Net.WebSockets`]('https://msdn.microsoft.com/en-us/library/system.net.websockets(v=vs.110).aspx') is not fully implemented on Windows 7.
See [this repository](https://github.com/ably-forks/ably-dotnet-alternative-transports) for a working example using the [websocket4net library](https://github.com/kerryjiang/WebSocket4Net).

To work with the (deprecated) UWP platform please use .NET Standard 2.0+ target.

&ast;&ast; We regression-test the library against .NET Core 3.1 and .NET Framework 4.6.2. If you find any compatibility issues, please do [raise an issue](https://github.com/ably/ably-dotnet/issues) in this repository or contact Ably customer support for advice. Any known runtime incompatibilities can be found [here](https://github.com/ably/ably-dotnet/issues?q=is%3Aissue+is%3Aopen+label%3A%22compatibility%22).

## Partially supported platforms

The following platforms are supported, but have some shortcomings or considerations:

### Unity

Unity support is currently in beta. See below for details on why it's considered beta.

Shortcomings & considerations:

* This library is only tested manually on Unity for Windows. We do not yet have automated tests running on the Unity platform.
* Installation requires developers to import a custom Unity package that includes all of Ably's dependencies.

Unity Requirements:

- Unity 2018.2.0 or newer
- The following Unity Player settings must be applied:
  - Scripting Runtime Version should be '.NET 4.x Equivalent'
  - Api Compatibility Level should be '.NET Standard 2.0'

Please download the latest Unity package from the [GitHub releases page](https://github.com/ably/ably-dotnet/releases). All releases from 1.1.16 will include a Unity package as well.

Implementation note for Unity. The library creates a number of threads and all callbacks are executed on non UI threads. This makes it difficult to update UI elements inside any callback executed by Ably. To make it easier we still support capturing the `SynchronizationContext `and synchronizing callbacks to the UI thread. This is OK for smaller projects and can be enabled using the following `Client` option `CaptureCurrentSynchronizationContext`. Even though the setting is deprecated it will not be removed.

## Unsupported platforms

### .NET 6 and Visual Studio 2022

`.NET 6` and `Visual Studio 2022`, both currently in preview, at not supported at this time but will be supported when they are officially released.

### Portable Class Libraries

A Portable Class Library version is not available. Portable Class Libraries are now considered deprecated and shared code should be packaged in .NET Standard Libraries. More information can be found [here](https://docs.microsoft.com/en-us/xamarin/cross-platform/app-fundamentals/code-sharing).

## Push notification

The Ably.net library fully supports Ably's push notifications. The feature set consists of two distinct areas: [Push Admin](https://ably.com/documentation/general/push/admin), [Device Push Notifications](https://ably.com/documentation/realtime/push). 

The [Push Notifications Readme](PushNotifications.md) describes:
- How to setup Push notifications on Xamarin mobile apps
- How to use the Push Admin api to send push notifications directly to a devices or a client
- How to subscribe to channels that support push notification
- How to send Ably messages that include a notification

## Known Limitations

* Browser push notifications in Blazor are not supported.

## Documentation

Visit https://www.ably.io/documentation for a complete API reference and more examples.

## Installation

The client library is available as a [nuget package](https://www.nuget.org/packages/ably.io/).

You can install it from the Package Manager Console using this command

```
PM> Install-Package ably.io
```

or using the .NET CLI in your project directory using

```
$ dotnet add package ably.io
```

## Using the Realtime API

### Introduction

All examples assume a client has been created as follows:

```csharp
// Using basic auth with API key
var realtime = new AblyRealtime("<api key>");
```

```csharp
// Using token auth with token string
var realtime = new AblyRealtime(new ClientOptions { Token = "token" });
```

If you do not have an API key, [sign up for a free API key now](https://www.ably.io/signup)

### Connection

Connecting and observing connection state changes. By default the library automatically initializes a connection. 

```csharp
realtime.Connection.On(ConnectionEvent.Connected, args =>
{
    // Do stuff  
});

```
To disable the default automatic connect behavior of the library, set `AutoConnect = false` when initializing the client.

```csharp
var realtime = new AblyRealtime(new ClientOptions("<api key>") { AutoConnect = false });
// Some code
realtime.Connect();
```

Subscribing to connection state changes and observing errors:

```csharp
realtime.Connection.On(args =>
{
    var currentState = args.Current; // Current state the connection transitioned to
    var previousState = args.Previous; // Previous state
    var error = args.Reason; // If the connection error-ed the Reason object will be populated.
});
```

### Subscribing to a channel

Create a channel

```csharp
IRealtimeChannel channel = realtime.Channels.Get("test");
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
    var state = args.NewState; // Current channel State
    var error = args.Error; // If the channel error-ed it will be reflected here
});
```

or

```csharp
channel.On(ChannelState.Attached, args =>
{
    // Do stuff when channel is attached
});
```

### Subscribing to a channel in delta mode

Subscribing to a channel in delta mode enables [delta compression](https://www.ably.io/documentation/realtime/channels/channel-parameters/deltas). This is a way for a client to subscribe to a channel so that message payloads sent contain only the difference (ie the delta) between the present message and the previous message on the channel.

Request a Vcdiff formatted delta stream using channel options when you get the channel:

```csharp
var channelParams = new ChannelParams();
channelParams.Add("delta", "vcdiff");
var channelOptions = new ChannelOptions();
channelOptions.Params = channelParams;
IRealtimeChannel channel = ably.Channels.Get(ChannelName, channelOptions);
```

Beyond specifying channel options, the rest is transparent and requires no further changes to your application. The `message.Data` instances that are delivered to your `Action<Message>` handler continue to contain the values that were originally published.

If you would like to inspect the `Message` instances in order to identify whether the `Data` they present was rendered from a delta message from Ably then you can see if `Extras.Delta.Format` equals `"vcdiff"`.

### Publishing to a channel

The client support a callback and async publishing. The simplest way to publish is:

```csharp
channel.Publish("greeting", "Hello World!");
```

with a callback:

```csharp
channel.Publish("greeting", "Hello World!", (success, error) =>
{
    // If publish succeeded 'success' is 'true'
    // if publish failed 'success' is 'false' and 'error' will contain the specific error
});
```

and the async version which if you `await` it will complete when the message has been acknowledged or rejected by the Ably service:

```csharp
var result = await channel.PublishAsync("greeting", "Hello World!");
// You can check if the message failed
if (result.IsFailure)
{
    var error = result.Error; // The error reason can be accessed as well
}
```

### Getting channel history

Calling history returns a paginated list of message. The object is of type `PaginatedResult<Message>` and can be iterated through as a normal list.  

```csharp
var history = await channel.HistoryAsync();
// Loop through current history page
foreach (var message in history.Items)
{
    // Do something with message
}
// Get next page.
var nextPage = await history.NextAsync();
```

### Getting presence history

Getting presence history is similar to how message history works. You get back `PaginatedResult<PresenceMessage>` and can navigate or iterate through the page

```csharp
var presenceHistory = await channel.Presence.HistoryAsync();
// Loop through the presence messages
foreach (var presence in presenceHistory.Items)
{
    // Do something with the messages
}

var presenceNextPage = await presenceHistory.NextAsync();
```

### Symmetric end-to-end encrypted payloads on a channel

When a 128-bit or 256-bit key is provided to the library, all payloads are encrypted and decrypted automatically using that key on the channel. The secret key is never transmitted to Ably and thus it is the developer's responsibility to distribute a secret key to both publishers and subscribers.

```csharp
var secret = Crypto.GetRandomKey();
var encryptedChannel = realtime.Get("encrypted", new ChannelOptions(secret));
encryptedChannel.Subscribe(message =>
{
    var data = message.data; // Sensitive data (encrypted before published)
});
encryptedChannel.Publish("name (not encrypted)", "sensitive data (encrypted before published)");
```

## Using the REST API

### Introduction

The rest client provides a fully async wrapper around the Ably service web api.

All examples assume a client and/or channel has been created as follows:

```csharp
var client = new AblyRest("<api key>");
IRealtimeChannel channel = client.Channels.Get("test");
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
    // Do something with each message
}
// Get the next page
var nextHistoryPage = await historyPage.NextAsync();
```

### Current presence members on a channel

```csharp
var presence = await channel.Presence.GetAsync();
var first = presence.Items.FirstOrDefault();
var clientId = first.clientId; // 'clientId' of the first member present
var nextPresencePage = await presence.NextAsync();
foreach (var presenceMessage in nextPresencePage.Items)
{
    // Do stuff with next page presence messages
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
        // Return a 'TokenDetails' instance or a preferably a 'TokenRequest' string.
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

When a 128-bit or 256-bit key is provided to the library, all payloads are encrypted and decrypted automatically using that key on the channel. The secret key is never transmitted to Ably and thus it is the developer's responsibility to distribute a secret key to both publishers and subscribers.

```csharp
var secret = Crypto.GetRandomKey();
IRealtimeChannel encryptedChannel = client.Channels.Get("encryptedChannel", new ChannelOptions(secret));
await encryptedChannel.PublishAsync("name", "sensitive data"); // Data will be encrypted before publish
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

### Increase Transport send and receive buffers

In .NET Framework projects, we discovered issues with the .NET implementation of the web socket protocol during times of high load with large payloads (over 50kb). This is better described in https://github.com/ably/ably-dotnet/issues/446
To work around the problem, you need to adjust websocket library's buffer to it's maximum size of 64kb. Here is an example of how to do it. 

```csharp
var maxBufferSize = 64 * 1024;
var options = new ClientOptions();
var websocketOptions = new MsWebSocketOptions() { SendBufferInBytes = maxBufferSize, ReceiveBufferInBytes = maxBufferSize };
options.TransportFactory = new MsWebSocketTransport.TransportFactory(websocketOptions);
var realtime = new AblyRealtime(options);
```

### Examples
- More Examples can be found under ```examples``` directory.
- While working with console app, make sure to put explicit await for async methods.

</br>*Sample .NET Core implementation*

```csharp
using System;

using IO.Ably;

namespace testing_ably_console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var realtime = new AblyRealtime("<api key>");
            IRealtimeChannel channel = realtime.Channels.Get("test");
            await channel.PublishAsync("greeting", "Hello World!");
            Console.WriteLine("Farewell World!");
        }
    }
}
```

</br>*Sample .NET Framework implementation (when you don't have async main method)*

```csharp
using System;

using IO.Ably;

namespace testing_ably_console
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            Console.WriteLine("Hello World!");
            var realtime = new AblyRealtime("<api key>");
            IRealtimeChannel channel = realtime.Channels.Get("test");
            await channel.PublishAsync("greeting", "Hello World!");
        }
    }
}
```

## Dependencies

This library has dependencies that can differ depending on the target platform.
See [the nuget page](http://nuget.org/packages/ably.io/) for specifics.

## Support, feedback and troubleshooting

Please visit http://support.ably.io/ for access to our knowledge-base and to ask for any assistance.

You can also view the [community reported GitHub issues](https://github.com/ably/ably-dotnet/issues).

## Contributing

1. Fork it
2. Create your feature branch (`git checkout -b my-new-feature`)
3. Commit your changes (`git commit -am 'Add some feature'`)
4. Ensure you have added suitable tests and the test suite is passing
4. Push to the branch (`git push origin my-new-feature`)
5. Create a new Pull Request

## Building and Packaging

The build scripts are written using ```fake``` and need to be run on Windows with Visual Studio 2017 or Visual Studio 2019 installed. Fake and nuget.exe can be installed via [chocolatey](https://chocolatey.org)

```
choco install fake
choco install nuget.commandline
```

Running `.\build.cmd` will start the build process and run the tests. By default it runs the NetFramework tests. 
To run the Netcore build and tests you can run `.\build.cmd Test.NetStandard`

## Working from source

If you want to incorporate `ably-dotnet` into your project from source (perhaps to use a specific development branch) the simplest way to do so is to add references to the relevant ably-dotnet projects. The following steps are specific to Visual Studio 2017, but the principal should transfer to other IDEs

1. Clone this repository to your local system (`git clone --recurse-submodules https://github.com/ably/ably-dotnet.git`)
2. Open the solution you want to reference ably-dotnet from
3. In Solution Explorer right click the root node (it will be labelled Solution 'YourSolutionName')
4. Select Add > Existing Project from the context menu
5. Browse to the ably-dotnet repository and add ably-dotnet\src\IO.Ably.Shared\IO.Ably.Shared.shproj
6. Browse to the ably-dotnet repository and add the project that corresponds to your target platform, so if you are targeting .NET Framework (AKA Classic .NET) you would add ably-dotnet\src\IO.Ably.NETFramework\IO.Ably.NETFramework.csproj, if you are targeting .NET Core 2 then chose ably-dotnet\src\IO.Ably.NetStandard20\IO.Ably.NetStandard20.csproj and so on.
7. In any project that you want to use `ably-dotnet` you need to add a project reference, to do so:
    1. Find your project in Solution Explorer and expand the tree so that the Dependencies node is visible
    2. Right click Dependencies and select Add Reference
    3. In the dialogue that opens you should see a list of the projects in your solution. Check the box next to IO.Ably.NETFramework (or whatever version you are trying to use) and click OK.

## Spec

The dotnet library follows the Ably [`Client Library development guide`](https://docs.ably.io/client-lib-development-guide/features/). To ensure it is easier to look up whether a spec item has been implemented or not; we add a Trait attribute to tests that implement parts of the spec. The convention is to add `[Trait("spec", "spec tag")]` to unit tests. 

To get a list of all spec items that appear in the tests you can run a script located in the tools directory. 
You need to have .NET Core 3.1 installed. It works on Mac, Linux and Windows. Run `dotnet fsi tools/list-test-categories.fsx`. It will produce a `results.csv` file which will include all spec items, which file it was found and on what line.

## Release process

This library uses [semantic versioning](http://semver.org/). For each release, the following needs to be done:

1. Create a release branch named in the form `release/1.2.3`.
2. Run `unity-plugins-updater.cmd 1.2.3` to migrate latest `NetStandard release DLL's` under unity plugins.
3. Once `unity-plugins-updater.cmd` script is successfully run, commit the change. 
4. Run [`github_changelog_generator`](https://github.com/skywinder/Github-Changelog-Generator) to automate the update of the [CHANGELOG](./CHANGELOG.md). Once the `CHANGELOG` update has completed, manually change the `Unreleased` heading and link with the current version number such as `v1.2.3`. Also ensure that the `Full Changelog` link points to the new version tag instead of the `HEAD`. Commit this change.
5. Update the version number and commit that change.
6. Create a PR and gain approvals for it, then merge that to `main`.
7. Run `package.cmd` to create the nuget package. 
8. Run `nuget push ably.io.*.nupkg -Source https://www.nuget.org/api/v2/package` (a private nuget API Key is required to complete this step, more information on publishing nuget packages can be found [here](https://docs.microsoft.com/en-us/nuget/quickstart/create-and-publish-a-package))
9. Against `main`, add a tag for the version and push to origin such as `git tag 1.2.3 && git push origin 1.2.3`.
10. Visit [https://github.com/ably/ably-dotnet/tags](https://github.com/ably/ably-dotnet/tags) and `Add release notes` for the release including links to the changelog entry.
11. Visit github CI for latest build, open `Unity / Generate Ably Unity Package (push)` step in a new tab, click on `summary` and download `AblyUnity` artifact, unzip it and rename the file as `ably-unity-1.2.3.unitypackage` and upload it under release notes.
12.  Create the entry on the [Ably Changelog](https://changelog.ably.com/) (via [headwayapp](https://headwayapp.co/))
