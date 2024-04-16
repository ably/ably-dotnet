# ably-dotnet

[![NuGet version](https://badge.fury.io/nu/ably.io.svg)](https://badge.fury.io/nu/ably.io)
[![Windows - build and test](https://github.com/ably/ably-dotnet/actions/workflows/build-and-test-windows.yml/badge.svg)](https://github.com/ably/ably-dotnet/actions/workflows/build-and-test-windows.yml)
[![MacOS - build and test](https://github.com/ably/ably-dotnet/actions/workflows/build-and-test-macos.yml/badge.svg)](https://github.com/ably/ably-dotnet/actions/workflows/build-and-test-macos.yml)
[![Linux - build and test](https://github.com/ably/ably-dotnet/actions/workflows/build-and-test-linux.yml/badge.svg)](https://github.com/ably/ably-dotnet/actions/workflows/build-and-test-linux.yml)
[![Features](https://github.com/ably/ably-dotnet/actions/workflows/features.yml/badge.svg)](https://github.com/ably/ably-dotnet/actions/workflows/features.yml)

_[Ably](https://ably.com) is the platform that powers synchronized digital experiences in realtime. Whether attending an event in a virtual venue, receiving realtime financial information, or monitoring live car performance data – consumers simply expect realtime digital experiences as standard. Ably provides a suite of APIs to build, extend, and deliver powerful digital experiences in realtime for more than 250 million devices across 80 countries each month. Organizations like Bloomberg, HubSpot, Verizon, and Hopin depend on Ably’s platform to offload the growing complexity of business-critical realtime data synchronization at global scale. For more information, see the [Ably documentation](https://ably.com/docs)._

This is a .NET client library for Ably which targets the 2.0 client library specification. You can see the features this client supports in our [feature support matrix](https://sdk.ably.com/builds/ably/ably-dotnet/main/features/).

## Supported platforms

* [.NET Standard 2.0+](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0)
* .NET 6.x, 7.x, MAUI, check [MAUI config](#maui-configuration).
* .NET Framework 4.6.2+
* .NET (Core) 2.0+
* Mono 5.4+
* [Xamarin.Android 8.0+](https://developer.xamarin.com/releases/android/xamarin.android_8/xamarin.android_8.0/)
* [Xamarin.iOS 10.14+](https://developer.xamarin.com/releases/ios/xamarin.ios_10/xamarin.ios_10.14/)
* Xamarin.Mac 3.8+
* Unity 2019.x +, [check README](./unity/README.md)
* Push Notification, [check support section](#push-notification).

## Documentation

Visit [https://ably.com/docs](https://ably.com/docs) for a complete API reference and more examples.

## Installation

The client library is available as a [nuget package](https://www.nuget.org/packages/ably.io/).

You can install it from the Package Manager Console:

```shell
PM> Install-Package ably.io
```

or using the .NET CLI in your project directory:

```shell
dotnet add package ably.io
```

## Using the Realtime API

The Realtime library is typically used client-side in your applications. It maintains a persistent connection to Ably and is a stateful library. [Find out when you should use the REST or Realtime library](https://faqs.ably.com/should-i-use-the-rest-or-realtime-library).

### Instancing a Realtime client

Creating a Realtime client:

```csharp
// Using basic auth with API key
// Note in production, an API key should not be used in untrusted mobile/browser clients
var realtime = new AblyRealtime("<api key>");
```

```csharp
// Using token auth with token string
// Note this token is not renewable - a token callback should be used in production
var realtime = new AblyRealtime(new ClientOptions { Token = "token" });
```

If you do not have an API key, [sign up for a free API key now](https://ably.com/signup)

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

[Channels](https://ably.com/docs/realtime/channels?lang=javascript) are the medium through which messages are distributed. 

To create a channel object:

```csharp
IRealtimeChannel channel = realtime.Channels.Get("test");
```

Subscribe to all events published on that channel:

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

- When calling `channel.Publish` with a dotnet object instance as the message data, the library will use the Newtonsoft Json.NET library to serialize the message with default serialization settings.
- If you need to use custom seralization settings, you can apply the serialization yourself and send the resulting string as the message data:

```csharp
var serializedData = JsonConvert.SerializeObject(message,
    new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    });

var ablyMessage = new Message("name", serializedData) {Encoding = "json"};

channel.Publish(ablyMessage);
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

### Subscribing to a channel in delta mode

Subscribing to a channel in delta mode enables [delta compression](https://ably.com/docs/realtime/channels/channel-parameters/deltas). This is a way for a client to subscribe to a channel so that message payloads sent contain only the difference (ie the delta) between the present message and the previous message on the channel.

Request a `Vcdiff` formatted delta stream using channel options when you get the channel:

```csharp
var channelParams = new ChannelParams();
channelParams.Add("delta", "vcdiff");
var channelOptions = new ChannelOptions();
channelOptions.Params = channelParams;
IRealtimeChannel channel = ably.Channels.Get(ChannelName, channelOptions);
```

Beyond specifying channel options, the rest is transparent and requires no further changes to your application. The `message.Data` instances that are delivered to your `Action<Message>` handler continue to contain the values that were originally published.

If you would like to inspect the `Message` instances in order to identify whether the `Data` they present was rendered from a delta message from Ably then you can see if `Extras.Delta.Format` equals `"vcdiff"`.

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

### Executing callbacks on Main/UI thread
- The library creates a number of threads and all listeners/callbacks are executed on non-UI/background threads.
- To execute listeners/callbacks on the Main/UI thread, we support capturing the `SynchronizationContext`.

```
options.CustomContext = SynchronizationContext.Current;
```

### Increase Transport send and receive buffers

In .NET Framework projects, we discovered issues with the .NET implementation of the web socket protocol during times of high load with large payloads (over 50kb). This is better described in `https://github.com/ably/ably-dotnet/issues/446`
To work around the problem, you need to adjust websocket library's buffer to it's maximum size of 64kb. Here is an example of how to do it.

```csharp
var maxBufferSize = 64 * 1024;
var options = new ClientOptions();
var websocketOptions = new MsWebSocketOptions() { SendBufferInBytes = maxBufferSize, ReceiveBufferInBytes = maxBufferSize };
options.TransportFactory = new MsWebSocketTransport.TransportFactory(websocketOptions);
var realtime = new AblyRealtime(options);
```

## Enable logging

Define a new class that implements `ILoggerSink` interface.

```csharp
class CustomLogHandler : ILoggerSink
{
    public void LogEvent(LogLevel level, string message)
    {
        Console.WriteLine($"Handler LogLevel : {level}, Data :{message}");
    }
}
```

Update clientOptions for `LogLevel` and `LogHandler`.

```csharp
clientOpts.LogLevel = LogLevel.Debug;
clientOpts.LogHandler = new CustomLogHandler();
```

## Using the REST API

The REST library is typically used server-side in your applications and is stateless. [Find out when you should use the REST or Realtime library](https://faqs.ably.com/should-i-use-the-rest-or-realtime-library).

### Instancing a REST client

Creating a REST client and channel:

```csharp
var client = new AblyRest("<api key>");
IRealtimeChannel channel = client.Channels.Get("chat");
```

If you do not have an API key, [sign up for a free API key now](https://ably.com/signup)

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

- When calling `channel.PublishAsync` with a dotnet object instance as the message data, the library will use the Newtonsoft Json.NET library to serialize the message with default serialization settings.
- If you need to use custom seralization settings, you can apply the serialization yourself and send the resulting string as the message data:

```csharp
var serializedData = JsonConvert.SerializeObject(message,
    new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    });

var ablyMessage = new Message("name", serializedData) {Encoding = "json"};

channel.Publish(ablyMessage);
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

### Authentication
- It is recommended to use `ABLY_KEY` at server side. Check [official ably auth documentation](https://ably.com/docs/auth) for more info.
- `ABLY_KEY` should not be exposed at client side where it can be used for unintentional purposes.
- Server can use `ABLY_KEY` for initializing the `AblyRest` instance.

```csharp
var rest = new AblyRest("API_KEY");
```
- Token requests are issued by your servers and signed using your private API key. 
- This is the preferred method of authentication as no secrets are ever shared, and the token request can be issued to trusted clients without communicating with Ably.
- Tokens issued using `tokenRequest` can only be used once by the client.
e.g. ASP.Net server app
```csharp
app.MapGet("/token", async() => {
    string tokenRequest = await rest.Auth.CreateTokenRequestAsync();
    return Content(tokenRequest, "application/json"); // make sure to set `contentType` as json.
});
```
- You can also return JWT string token signed using `ABLY_KEY` as per [official ably JWT doc](https://ably.com/tutorials/jwt-authentication). 
- Unline tokenrequests, JWT token can be used by the client multiple times till it's 'expiry.

### Using the Token auth at client side

- To use `AuthCallback` create a `ClientOptions` instance and assign an appropriate delegate to the `AuthCallback` property and pass the `ClientOptions` to a new `AblyRealtime` instance.

```csharp
var options = new ClientOptions
{
    AuthCallback = async tokenParams =>
    {
        // Return serialized tokenRequest string or 'IO.Ably.TokenRequest' object
        return await TokenRequestStringFromYourServer(tokenParams); // tokenRequest will be used to obtain token from ably server.
    }
};
var client = new AblyRealtime(options);
```
- If JWT token is returned by server 
```csharp
var options = new ClientOptions
{
    AuthCallback = async tokenParams =>
    {
        // Return serialized jwttokenstring returned from server
        string jwtToken = await getJwtTokenFromServer(tokenParams);
        return await new TokenDetails(jwtToken); // jwt token will directly be used to authenticate with server.
    }
};
```
- Please note that there is no intermediate step to obtain token from server, jwt will directly be used to authenticate with ably.
- Do check [official token auth documentation](https://ably.com/docs/auth/token?lang=csharp) for more information.

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

### Getting the channel status

Getting the current status of a channel, including details of the current number of `Publishers`, `Subscribers` and `PresenceMembers` etc:

```csharp
ChannelDetails details = await channel.StatusAsync();
ChannelMetrics metrics = details.Status.Occupancy.Metrics;
// Do something with 'metrics.Publishers' etc
```

### Making explicit HTTP requests to Ably Rest Endpoints / Batch publish
- The `AblyRest->Request` method can be used to make explicit HTTP requests to the [Ably REST API](https://ably.com/docs/api/rest-api).
- It automatically adds necessary auth headers based on the initial auth config and supports pagination.
- The following is an example of using the batch publish API based on the [Ably batch publish rest endpoint documentation](https://ably.com/docs/api/rest-api#batch-publish).

```csharp
    var objectPayload = new
    {
        channels = new[] { "channel1", "channel2", "channel3", "channel4" },
        messages = new[]
        {
            new
            {
                name = "eventName",
                data = "foo",
            }
        }
    };
  var jsonPayload = JsonConvert.SerializeObject(objectPayload);
  var paginatedResponse = await ablyRest.RequestV2(HttpMethod.Post, "/messages", null, jsonPayload, null);
```
- See the [ably rest endpoint doc](https://ably.com/docs/api/rest-api) for more information on other endpoints.

## Examples

* More Examples can be found under ```examples``` directory.
* While working with console app, make sure to put explicit await for async methods.

### Sample .NET Core implementation

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

### Sample .NET Framework implementation (when you don't have async main method)*

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

## MAUI configuration
- Since `ably-dotnet` makes use of the reflection API, MAUI assembly trimming may cause issues.
- When using MAUI, we recommend adding the following to your `.csproj` file to disable assembly trimming.

```xml
<ItemGroup>
  <TrimmerRootAssembly Include="IO.Ably" />
</ItemGroup>
```
- For more information related to assembly trimming, check [MAUI trimming doc](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trimming-options).
- To resolve issues related to iOS signed release, [update csproj config](https://github.com/ably/ably-dotnet/issues/1259#issuecomment-1723307985)

## Push notification

The Ably.net library fully supports Ably's push notifications. The feature set consists of two distinct areas: [Push Admin](https://ably.com/docs/general/push/admin), [Device Push Notifications](https://ably.com/docs/realtime/push).

The [Push Notifications Readme](PushNotifications.md) describes:

* How to setup Push notifications for Xamarin mobile apps.
* How to use the Push Admin api to send push notifications directly to a devices or a client.
* How to subscribe to channels that support push notification.
* How to send Ably messages that include a notification.

### Known Limitations
* Browser push notifications in [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor) are not supported.


## Dependencies

This library has dependencies that can differ depending on the target platform.
See [the nuget page](http://nuget.org/packages/ably.io/) for specifics.

## Support, feedback and troubleshooting

Please visit `https://ably.com/support` for access to our knowledge-base and to ask for any assistance.

You can also view the [community reported GitHub issues](https://github.com/ably/ably-dotnet/issues).

## Contributing

For guidance on how to contribute to this project, see [CONTRIBUTING.md](CONTRIBUTING.md).
