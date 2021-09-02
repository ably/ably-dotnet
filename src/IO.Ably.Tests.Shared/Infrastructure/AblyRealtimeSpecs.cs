using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Tests.Realtime;
using IO.Ably.Types;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public abstract class AblyRealtimeSpecs : MockHttpRestSpecs, IDisposable
    {
        protected const string TestChannelName = "test";

        private readonly AutoResetEvent _signal = new AutoResetEvent(false);

        protected AblyRealtimeSpecs(ITestOutputHelper output)
            : base(output)
        {
            FakeTransportFactory = new FakeTransportFactory();
        }

        public List<AblyRealtime> RealtimeClients { get; set; } = new List<AblyRealtime>();

        protected FakeTransportFactory FakeTransportFactory { get; private set; }

        protected ProtocolMessage ConnectedProtocolMessage =>
            new ProtocolMessage(ProtocolMessage.MessageAction.Connected)
            {
                ConnectionDetails = new ConnectionDetails { ConnectionKey = "connectionKey" },
                ConnectionId = "1",
                ConnectionSerial = 100
            };

        public void Dispose()
        {
            foreach (var client in RealtimeClients)
            {
                try
                {
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    Output?.WriteLine("Error disposing Client: " + ex.Message);
                }
            }

            _signal?.Dispose();
        }

        internal virtual AblyRealtime GetRealtimeClient(ClientOptions options = null, Func<AblyRequest, Task<AblyResponse>> handleRequestFunc = null)
        {
            var clientOptions = options ?? new ClientOptions(ValidKey);
            clientOptions.SkipInternetCheck = true; // This is for the Unit tests
            var client = new AblyRealtime(clientOptions, opts => GetRestClient(handleRequestFunc, clientOptions));
            return client;
        }

        internal virtual AblyRealtime GetRealtimeClientWithFakeMessageHandler(ClientOptions options = null, FakeHttpMessageHandler fakeMessageHandler = null)
        {
            var clientOptions = options ?? new ClientOptions(ValidKey);
            clientOptions.SkipInternetCheck = true; // This is for the Unit tests
            var client = new AblyRealtime(clientOptions);
            if (fakeMessageHandler != null)
            {
                client.RestClient.HttpClient.CreateInternalHttpClient(TimeSpan.FromSeconds(10), fakeMessageHandler);
            }

            return client;
        }

        internal virtual AblyRealtime GetRealtimeClient(Action<ClientOptions> optionsAction, Func<AblyRequest, Task<AblyResponse>> handleRequestFunc = null)
        {
            var options = new ClientOptions(ValidKey);
            options.SkipInternetCheck = true; // This is for the Unit tests
            optionsAction?.Invoke(options);

            var client = new AblyRealtime(options, clientOptions => GetRestClient(handleRequestFunc, clientOptions));
            return client;
        }

        protected FakeTransport LastCreatedTransport => FakeTransportFactory.LastCreatedTransport;

        internal AblyRealtime GetClientWithFakeTransport(Action<ClientOptions> optionsAction = null, Func<AblyRequest, Task<AblyResponse>> handleRequestFunc = null)
        {
            var options = new ClientOptions(ValidKey) { TransportFactory = FakeTransportFactory };
            optionsAction?.Invoke(options);
            var client = GetRealtimeClient(options, handleRequestFunc);
            return client;
        }

        internal AblyRealtime GetClientWithFakeTransportAndMessageHandler(Action<ClientOptions> optionsAction = null, FakeHttpMessageHandler messageHandler = null)
        {
            var options = new ClientOptions(ValidKey) { TransportFactory = FakeTransportFactory };
            optionsAction?.Invoke(options);
            var client = GetRealtimeClientWithFakeMessageHandler(options, messageHandler);
            return client;
        }

        internal async Task<AblyRealtime> GetConnectedClient(Action<ClientOptions> optionsAction = null, Func<AblyRequest, Task<AblyResponse>> handleRequestFunc = null)
        {
            var client = GetClientWithFakeTransport(optionsAction, handleRequestFunc);
            client.FakeProtocolMessageReceived(ConnectedProtocolMessage);
            await client.WaitForState(ConnectionState.Connected);
            return client;
        }

        protected void WaitOne()
        {
            var result = _signal.WaitOne(2000);
            result.Should().BeTrue("Result was not returned within 2000ms");
        }

        protected void Done()
        {
            _signal.Set();
        }

        protected AblyRealtime GetDisconnectedClient(ClientOptions options = null)
        {
            var clientOptions = options ?? new ClientOptions(ValidKey);

            clientOptions.AutoConnect = false;

            return GetRealtimeClient(clientOptions);
        }

        protected IDisposable EnableDebugLogging()
        {
            Logger.LoggerSink = new SandboxSpecs.OutputLoggerSink(Output);
            Logger.LogLevel = LogLevel.Debug;

            return new ActionOnDispose(() =>
            {
                Logger.LoggerSink = new DefaultLoggerSink();
                Logger.LogLevel = LogLevel.Warning;
            });
        }

        protected Task<IRealtimeChannel> GetChannel(Action<ClientOptions> optionsAction = null) => GetConnectedClient(optionsAction).MapAsync(client => client.Channels.Get("test"));

        protected Task<(AblyRealtime, IRealtimeChannel)> GetClientAndChannel(Action<ClientOptions> optionsAction = null) =>
            GetConnectedClient(optionsAction).MapAsync(x => (x, x.Channels.Get("test")));

        protected Task<IRealtimeChannel> GetTestChannel(IRealtimeClient client = null, Action<ClientOptions> optionsAction = null, ChannelOptions channelOptions = null)
        {
            if (client == null)
            {
                return GetConnectedClient().MapAsync(x => x.Channels.Get(TestChannelName, channelOptions));
            }

            return Task.FromResult(client.Channels.Get(TestChannelName, channelOptions));
        }
    }
}
