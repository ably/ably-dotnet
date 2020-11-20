# ably-dotnet

[![NuGet version](https://badge.fury.io/nu/ably.io.svg)](https://badge.fury.io/nu/ably.io)
[![NetFramework build status](https://dev.azure.com/vayadigital/Ably%20Realtime/_apis/build/status/ably.ably-dotnet?branchName=main)](https://dev.azure.com/vayadigital/Ably%20Realtime/_build/latest?definitionId=1&branchName=main)
[![NetStandard build status](https://dev.azure.com/vayadigital/Ably%20Realtime/_apis/build/status/ably.ably-dotnet%20(1)?branchName=main)](https://dev.azure.com/vayadigital/Ably%20Realtime/_build/latest?definitionId=2&branchName=main)

A .NET client library for [www.ably.io](https://www.ably.io), the realtime messaging service. This library currently targets the [Ably 1.1-beta client library specification](https://www.ably.io/documentation/client-lib-development-guide/features/). You can jump to the '[Known Limitations](#known-limitations)' section to see the features this client library does not yet support or or [view our client library SDKs feature support matrix](https://www.ably.io/download/sdk-feature-support-matrix) to see the list of all the available features.

## Xamarin and VS 2017

There is an open issue for versions 1.1.14 and above when working with Xamarim Projects and Visual Studio 2017. Ably (1.1.14) was compiled using MsBuild that came with VS 2019 which causes an issue when a Xamarin app is compiled using VS 2017. More information can be found in this [Stackoverflow post](https://stackoverflow.com/questions/58032635/updating-nuget-caused-exception-unhandled-system-typeloadexception/58064929#58064929). 
Until we resolve the issue you can either use version 1.1.13 or update to using Visual Studio 2019. Please create an support issue if this is causing problems. 


## Significant changes in 1.1.15

Version 1.1.15 has seen a significant rewrite of the library internals which was needed to make the library safer and provide a good basis for implementing the rest of the spec. 
Here is a list of the significant changes. You can find a full list in the release notes.

1. [Breaking]Presence and IRealtimeChannel no longer implement the IDisposable interface. They don't hold on to any unmanaged recourses and there was no need to expose the Dispose function. 
2. [Breaking]ITransport has acquired an Id Property and ITransportListener.OnTransportEvent has an Id parameter. This is needed because we need to distinguish events raised different Transport instances. Sometimes the Closed event doesn't get processed until another transport has already been instantiated. 
3. `ClientOptions.CaptureCurrentSynchronizationContext` has been deprecated and defaulted to `false`. It will be removed in future versions. You need to make sure that you don't directly update UI elements if you are building a WPF or Xamarin.Forms application from Ably handlers. If you still require the functionality please set it back to `true` and open an Ably Support ticket that you need the functionality. The main reason to disable this feature is that the library should not assume on which thread updates should be posted and that needs to be handled by the developer.
4. IRealtimeClient implements IDisposable - If you want to clean up after the library you can now safely call `Dispose()`. Please note that you can no longer use this instance and have to create a new one.
5. Logging has been greatly improved. We've removed a lot of verbose messages that brought little value. There is a helpful debug method called `.GetCurrentState()` on the realtime client that will dump the whole library's state as a json string. This will be helpful to include in the support tickets.


## Supported platforms

* .NET 4.6.2+ &ast;
* .NET Core &ast;&ast;
* .NET Standard 2.0+
* Mono 5.4+
* UWP
* [Xamarin.Android 8.0+](https://developer.xamarin.com/releases/android/xamarin.android_8/xamarin.android_8.0/)
* [Xamarin.iOS 11.4+](https://developer.xamarin.com/releases/ios/xamarin.ios_11/xamarin.ios_11.4/)

&ast; To target Windows 7 (with .Net 4.6) a custom [ITransportFactory](https://github.com/ably/ably-dotnet/blob/main/src/IO.Ably.Shared/Transport/ITransport.cs) will need to be implemented in your project that uses an alternate Web Socket library. 
This is because [System.Net.WebSockets]('https://msdn.microsoft.com/en-us/library/system.net.websockets(v=vs.110).aspx') is not fully implemented on Windows 7.
See [this repository](https://github.com/ably-forks/ably-dotnet-alternative-transports) for a working example using the [websocket4net library](https://github.com/kerryjiang/WebSocket4Net).

&ast;&ast; We regression-test the library against .NET Core 2 and .Net Framework 4.6.2. If you find any compatibility issues, please do [raise an issue](https://github.com/ably/ably-dotnet/issues) in this repository or contact Ably customer support for advice. Any known runtime incompatibilities can be found [here](https://github.com/ably/ably-dotnet/issues?q=is%3Aissue+is%3Aopen+label%3A%22compatibility%22).

### Partial platform support

The following platforms are supported, but have some shortcomings or considerations:

#### Unity

Unity support is currently in beta. See below for details on why it's considered beta.

Shortcomings & considerations:

* This library is only tested manually on Unity for Windows. We do not yet have automated tests running on the Unity platform.
* Installation requires developers to import a custom unity packages that includes all of Ably's dependencies.

Unity Requirements:

- Unity 2018.2.0 or newer
- The following Unity Player settings must be applied:
  - Scripting Runtime Version should be '.NET 4.x Equivalent'
  - Api Compatibility Level should be '.NET Standard 2.0'

Please download the latest unity package from the [github releases page](https://github.com/ably/ably-dotnet/releases). All releases from 1.1.16 will include a unity package as well.

Implementation note for Unity. The library creates a number of threads and all callbacks are executed on non UI threads. This makes it difficult to update UI elements inside any callback executed by Ably. To make it easier we still support capturing the SynchronizationContext and synchronizing callbacks to the UI thread. This is OK for smaller projects and can be enabled using the following Client option `CaptureCurrentSynchronizationContext`. Even thought the setting is deprecated it will not be removed.

### Unsupported platforms

A portable class library (PCL) version is not available. See [this comment](https://github.com/ably/ably-dotnet/issues/182#issuecomment-366939087) for more information on this choice and the potential workarounds that are available. 

## Known Limitations

This client library is currently *not compatible* with some of the Ably features:

| Feature | 
|:--- |
| [Push Notification target](https://www.ably.io/documentation/general/push/activate-subscribe#subscribing) |
| [Push Notification admin](https://www.ably.io/documentation/general/push/admin) |
| [Custom transportParams](https://www.ably.io/documentation/realtime/usage#client-options) |
| [Message extras](https://www.ably.io/documentation/realtime/types#message) |

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

### Subscribing to a channel in delta mode

Subscribing to a channel in delta mode enables [delta compression](https://www.ably.io/documentation/realtime/channels/channel-parameters/deltas). This is a way for a client to subscribe to a channel so that message payloads sent contain only the difference (ie the delta) between the present message and the previous message on the channel.

Request a Vcdiff formatted delta stream using channel options when you get the channel:

```csharp
var channelParams = new ChannelParams();
channelParams.Add("delta", "vcdiff");
var channelOptions = new ChannelOptions();
channelOptions.Params = channelParams;
var channel = ably.Channels.Get(ChannelName, channelOptions);
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

### Increase Transport send and receive buffers

In .Net Framework projects, we discovered issues with the .Net implementation of web socket protocol during times of high load with large payloads (over 50kb). This is better described in https://github.com/ably/ably-dotnet/issues/446
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
- While working with console app, make sure to put explicit await for async methods.</br>
*Sample .net core implementation*
```C#
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
            var channel = realtime.Channels.Get("test");
            await channel.PublishAsync("greeting", "Hello World!");
            Console.WriteLine("Farewell World!");
        }
    }
}
```
</br>*Sample .net framework implementation (when you don't have async main method)*
```C#
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
            var channel = realtime.Channels.Get("test");
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

You can also view the [community reported Github issues](https://github.com/ably/ably-dotnet/issues).

## Contributing

1. Fork it
2. Create your feature branch (`git checkout -b my-new-feature`)
3. Commit your changes (`git commit -am 'Add some feature'`)
4. Ensure you have added suitable tests and the test suite is passing
4. Push to the branch (`git push origin my-new-feature`)
5. Create a new Pull Request

## Building and Packaging

The build scripts are written using ```fake``` and need to be run on Windows with Visual Studio 2017 installed. Fake and nuget.exe can be installed via [chocolatey](https://chocolatey.org)

    choco install fake
    choco install nuget.commandline

Running `.\build.cmd` will start the build process and run the tests. By default it runs the NetFramework tests. 
To run the Netcore build and tests you can run `.\build.cmd Test.NetStandard`

## Working from source

If you want to incorporate ably-dotnet into your project from source (perhaps to use a specific development branch) the simplest way to do so is to add references to the relevant ably-dotnet projects. The following steps are specific to Visual Studio 2017, but the pricipal should transfer to other IDEs

1. Clone this repository to your local system
2. Open the solution you want to reference ably-dotnet from
3. In Solution Explorer right click the root note (it will be labled Solution 'YourSolutionName')
4. Select Add > Existing Project from the context menu
5. Browse to the ably-dotnet repository and add ably-dotnet\src\IO.Ably.Shared\IO.Ably.Shared.shproj
6. Browse to the ably-dotnet repository and add the project that corresponds to your target platform, so if you are targetting .Net Framework (AKA Classic .Net) you would add ably-dotnet\src\IO.Ably.NETFramework\IO.Ably.NETFramework.csproj, if you are targeting .NET Core 2 then chose ably-dotnet\src\IO.Ably.NetStandard20\IO.Ably.NetStandard20.csproj and so on.
7. In any project that you want to use ably-dotnet you need to add a project reference, to do so:
    1. Find your project in Solution Explorer and expand the tree so that the Dependencies node is visible
    2. Right click Dependencies and select Add Reference
    3. In the dialogue that opens you should see a list of the projects in your solution. Check the box next to IO.Ably.NETFramework (or whatever version you are trying to use) and click OK.

## Spec

The dotnet library follows the Ably [`Client Library development guide`](https://docs.ably.io/client-lib-development-guide/features/). To ensure it is easier to look up whether a spec item has been implemented or not; we add a Trait attribute to tests that implement parts of the spec. The convertion is to add `[Trait("spec", "spec tag")]` to unit tests. 

To get a list of all spec items that appear in the tests you can run a script located in the tools directory. 
You need to have .net core 3.0 installed. It works on Mac, Linux and Windows. Run `dotnet fsi tools/list-test-categories.fsx`. It will produce a `results.csv` file which will include all spec items, which file it was found and on what line.


## Release process

This library uses [semantic versioning](http://semver.org/). For each release, the following needs to be done:

* Run [`github_changelog_generator`](https://github.com/skywinder/Github-Changelog-Generator) to automate the update of the [CHANGELOG](./CHANGELOG.md). Once the `CHANGELOG` update has completed, manually change the `Unreleased` heading and link with the current version number such as `v1.0.0`. Also ensure that the `Full Changelog` link points to the new version tag instead of the `HEAD`. Commit this change.
* Add a tag for the version and push to origin such as `git tag 1.0.0 && git push origin 1.0.0`. For beta versions the version string should be `Maj.Min.Patch-betaN`, e.g `1.0.0-beta1`
* Visit [https://github.com/ably/ably-dotnet/tags](https://github.com/ably/ably-dotnet/tags) and `Add release notes` for the release including links to the changelog entry.
* Run `package.cmd` to create the nuget package. 
* Run `nuget push ably.io.*.nupkg -Source https://www.nuget.org/api/v2/package` (a private nuget API Key is required to complete this step, more information on publishing nuget packages can be found [here](https://docs.microsoft.com/en-us/nuget/quickstart/create-and-publish-a-package))
 