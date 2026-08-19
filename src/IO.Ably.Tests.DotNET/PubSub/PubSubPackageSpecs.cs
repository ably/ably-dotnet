using System;
using FluentAssertions;
using IO.Ably.PubSub.Device;
using IO.Ably.PubSub.Server;
using Xunit;

namespace IO.Ably.Tests.PubSub
{
    /// <summary>
    /// Specs for the per-side PubSub packages. They are thin factories over the core clients, so these
    /// specs check that each door hands back the right kind of client and loses nothing the caller
    /// configured on the way through.
    /// </summary>
    public class PubSubPackageSpecs
    {
        private const string ValidKey = "1iZPfA.BjcI_g:wpNhw5RCw6rDjisl";

        // Realtime clients connect on construction unless told otherwise, which a unit test must not do.
        private static ClientOptions Options() => new ClientOptions(ValidKey) { AutoConnect = false };

        [Fact]
        public void DeviceCreateClient_ShouldReturnARealtimeClientForTheGivenKey()
        {
            using var fromKey = PubSubDevice.CreateClient(ValidKey);
            using var fromOptions = PubSubDevice.CreateClient(Options());

            fromKey.Should().BeOfType<AblyRealtime>();
            fromKey.Options.Key.Should().Be(ValidKey);
            fromOptions.Options.Key.Should().Be(ValidKey);
        }

        [Fact]
        public void ServerCreateRealtimeClient_ShouldReturnARealtimeClientForTheGivenKey()
        {
            using var client = PubSubServer.CreateRealtimeClient(Options());

            client.Should().BeOfType<AblyRealtime>();
            client.Options.Key.Should().Be(ValidKey);
        }

        [Fact]
        public void ServerCreateHttpClient_ShouldReturnAnHttpClientForTheGivenKey()
        {
            var client = PubSubServer.CreateHttpClient(ValidKey);

            client.Should().BeOfType<AblyRest>();
            client.Options.Key.Should().Be(ValidKey);
        }

        [Fact]
        public void Factories_ShouldUseTheOptionsInstanceTheCallerSupplied()
        {
            var options = Options();
            options.ClientId = "caller-1";

            using var realtime = PubSubServer.CreateRealtimeClient(options);
            var http = PubSubServer.CreateHttpClient(options);

            realtime.Options.Should().BeSameAs(options);
            http.Options.Should().BeSameAs(options);
            realtime.Options.ClientId.Should().Be("caller-1");
        }

        [Fact]
        public void ActionOverloads_ShouldApplyCallerConfiguration()
        {
            using var device = PubSubDevice.CreateClient(options =>
            {
                options.Key = ValidKey;
                options.AutoConnect = false;
                options.ClientId = "device-1";
            });

            var server = PubSubServer.CreateHttpClient(options =>
            {
                options.Key = ValidKey;
                options.ClientId = "server-1";
            });

            device.Options.ClientId.Should().Be("device-1");
            server.Options.ClientId.Should().Be("server-1");
        }

        [Fact]
        public void Factories_ShouldThrow_WhenGivenNoOptions()
        {
            Assert.Throws<ArgumentNullException>(() => PubSubDevice.CreateClient((ClientOptions)null));
            Assert.Throws<ArgumentNullException>(() => PubSubDevice.CreateClient((Action<ClientOptions>)null));
            Assert.Throws<ArgumentNullException>(() => PubSubServer.CreateRealtimeClient((ClientOptions)null));
            Assert.Throws<ArgumentNullException>(() => PubSubServer.CreateRealtimeClient((Action<ClientOptions>)null));
            Assert.Throws<ArgumentNullException>(() => PubSubServer.CreateHttpClient((ClientOptions)null));
            Assert.Throws<ArgumentNullException>(() => PubSubServer.CreateHttpClient((Action<ClientOptions>)null));
        }
    }
}
