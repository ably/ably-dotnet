using System;
using FluentAssertions;
using IO.Ably.Push;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Push
{
    public class PushChannelTests
    {
        [Trait("spec", "RSH7")]
        public class WhenPlatformSupportsPushNotifications : AblyRealtimeSpecs
        {
            [Fact]
            public void RealtimeChannel_ShouldInitialisePushChannel()
            {
                var channel = GetRealtimeClient().Channels.Get("test");
                channel.Push.Should().NotBeNull();
                channel.Push.ChannelName.Should().Be(channel.Name);
            }

            [Fact]
            public void RestChannel_ShouldInitialisePushChannel()
            {
                var restClient = GetRealtimeClient().RestClient;
                var channel = restClient.Channels.Get("test");
                channel.Push.Should().NotBeNull();
                channel.Push.ChannelName.Should().Be(channel.Name);
            }

            [Fact]
            public void RestChannel_WhenRestClientInitialisedDirectly_ShouldInitialisePushChannel()
            {
                var channel = GetRestClient().Channels.Get("test");
                channel.Push.Should().NotBeNull();
                channel.Push.ChannelName.Should().Be(channel.Name);
            }

            private AblyRealtime GetRealtimeClient()
            {
                return new AblyRealtime(new ClientOptions(ValidKey) { AutoConnect = false }, mobileDevice: new FakeMobileDevice());
            }

            private AblyRest GetRestClient()
            {
                return new AblyRest(new ClientOptions(ValidKey), mobileDevice: new FakeMobileDevice());
            }

            public WhenPlatformSupportsPushNotifications(ITestOutputHelper output)
                : base(output)
            {
            }
        }

        [Trait("spec", "RSH7")]
        public class WhenPlatformDoesNotSupportPushNotifications : AblyRealtimeSpecs
        {
            [Fact]
            public void RealtimeChannel_ShouldThrowWhenAccessingPushProperty()
            {
                var channel = GetRealtimeClient().Channels.Get("test");
                Func<PushChannel> getPushChannel = () => channel.Push;

                getPushChannel.Should().Throw<AblyException>();
            }

            [Fact]
            public void RestChannel_ShouldInitialisePushChannel()
            {
                var restClient = GetRealtimeClient().RestClient;
                var channel = restClient.Channels.Get("test");
                Func<PushChannel> getPushChannel = () => channel.Push;

                getPushChannel.Should().Throw<AblyException>();
            }

            [Fact]
            public void RestChannel_WhenRestClientInitialisedDirectly_ShouldInitialisePushChannel()
            {
                var channel = GetRestClient().Channels.Get("test");
                Func<PushChannel> getPushChannel = () => channel.Push;

                getPushChannel.Should().Throw<AblyException>();
            }

            private AblyRealtime GetRealtimeClient()
            {
                return new AblyRealtime(new ClientOptions(ValidKey) { AutoConnect = false });
            }

            private AblyRest GetRestClient()
            {
                return new AblyRest(new ClientOptions(ValidKey));
            }

            public WhenPlatformDoesNotSupportPushNotifications(ITestOutputHelper output)
                : base(output)
            {
            }
        }
    }
}
