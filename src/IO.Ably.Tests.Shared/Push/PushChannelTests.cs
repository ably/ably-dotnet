using System;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Push;
using IO.Ably.Tests.Infrastructure;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Push
{
    public static class PushChannelTests
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

        public class GeneralTests : MockHttpRestSpecs
        {
            [Fact]
            [Trait("spec", "RSH7a")]
            [Trait("spec", "RSH7a1")]
            public void SubscribeDevice_ShouldThrowWhenLocalDeviceIsMissingDeviceIdentityToken()
            {
                var client = GetRestClient(mobileDevice: new FakeMobileDevice());
                client.Device = new LocalDevice();
                var pushChannel = client.Channels.Get("test").Push;

                Func<Task> subscribeFunc = () => pushChannel.SubscribeDevice();

                subscribeFunc.Should().ThrowAsync<AblyException>().WithMessage("Cannot Subscribe device to channel*");
            }

            [Fact]
            [Trait("spec", "RSH7a")]
            [Trait("spec", "RSH7a2")]
            public async Task SubscribeDevice_ShouldSendARequestTo_PushChannelSubscriptions_WithCorrectParameters()
            {
                const string channelName = "testChannel";
                const string deviceId = "deviceId";

                var taskAwaiter = new TaskCompletionAwaiter();

                async Task<AblyResponse> RequestHandler(AblyRequest request)
                {
                    request.Url.Should().Be("/push/channelSubscriptions");
                    var postData = (PushChannelSubscription)request.PostData;
                    postData.Should().NotBeNull();
                    postData.Channel.Should().Be(channelName);
                    postData.DeviceId.Should().Be(deviceId);
                    postData.ClientId.Should().BeNullOrEmpty();

                    taskAwaiter.SetCompleted();
                    return new AblyResponse() { TextResponse = JsonConvert.SerializeObject(new PushChannelSubscription()) };
                }

                var client = GetRestClient(RequestHandler, mobileDevice: new FakeMobileDevice());

                client.Device = new LocalDevice()
                {
                    Id = deviceId,
                    DeviceIdentityToken = "identityToken"
                };
                var pushChannel = client.Channels.Get(channelName).Push;

                await pushChannel.SubscribeDevice();

                (await taskAwaiter).Should().BeTrue("Didn't validate function");
            }

            [Fact]
            [Trait("spec", "RSH7a")]
            [Trait("spec", "RSH7a3")]
            public async Task SubscribeDevice_ShouldUsePushDeviceAuthentication()
            {
                const string deviceIdentityToken = "identityToken";
                var taskAwaiter = new TaskCompletionAwaiter();

                async Task<AblyResponse> RequestHandler(AblyRequest request)
                {
                    request.Headers.Should().ContainKey(Defaults.DeviceIdentityTokenHeader).WhichValue.Should()
                        .Be(deviceIdentityToken);

                    taskAwaiter.SetCompleted();
                    return new AblyResponse()
                        { TextResponse = JsonConvert.SerializeObject(new PushChannelSubscription()) };
                }

                var client = GetRestClient(RequestHandler, mobileDevice: new FakeMobileDevice());

                client.Device = new LocalDevice()
                {
                    Id = "id",
                    DeviceIdentityToken = deviceIdentityToken
                };

                var pushChannel = client.Channels.Get("test").Push;

                await pushChannel.SubscribeDevice();

                (await taskAwaiter).Should().BeTrue("Didn't validate function");
            }

            [Fact]
            [Trait("spec", "RSH7b")]
            [Trait("spec", "RSH7b1")]
            public void SubscribeClient_ShouldThrowWhenLocalDeviceIsMissingDeviceIdentityToken()
            {
                var client = GetRestClient(mobileDevice: new FakeMobileDevice());
                client.Device = new LocalDevice();
                var pushChannel = client.Channels.Get("test").Push;

                Func<Task> subscribeFunc = () => pushChannel.SubscribeClient();

                subscribeFunc.Should().ThrowAsync<AblyException>().WithMessage("Cannot Subscribe device to channel*");
            }

            [Fact]
            [Trait("spec", "RSH7b")]
            [Trait("spec", "RSH7b1")]
            public void SubscribeClient_ShouldThrowWhenLocalDeviceIsMissingClientId()
            {
                var client = GetRestClient(mobileDevice: new FakeMobileDevice());
                client.Device = new LocalDevice() { DeviceIdentityToken = "token" };
                var pushChannel = client.Channels.Get("test").Push;

                Func<Task> subscribeFunc = () => pushChannel.SubscribeClient();

                subscribeFunc.Should().ThrowAsync<AblyException>()
                    .WithMessage("Cannot Subscribe clientId to channel*");
            }

            [Fact]
            [Trait("spec", "RSH7b")]
            [Trait("spec", "RSH7b2")]
            public async Task SubscribeChannel_ShouldSendARequestTo_PushChannelSubscriptions_WithCorrectParameters()
            {
                const string channelName = "testChannel";
                const string clientId = "client101";

                var taskAwaiter = new TaskCompletionAwaiter();

                async Task<AblyResponse> RequestHandler(AblyRequest request)
                {
                    request.Url.Should().Be("/push/channelSubscriptions");
                    var postData = (PushChannelSubscription)request.PostData;
                    postData.Should().NotBeNull();
                    postData.Channel.Should().Be(channelName);
                    postData.ClientId.Should().Be(clientId);
                    postData.DeviceId.Should().BeNullOrEmpty();

                    taskAwaiter.SetCompleted();
                    return new AblyResponse() { TextResponse = JsonConvert.SerializeObject(new PushChannelSubscription()) };
                }

                var client = GetRestClient(RequestHandler, mobileDevice: new FakeMobileDevice());

                client.Device = new LocalDevice()
                {
                    ClientId = clientId,
                    DeviceIdentityToken = "identityToken"
                };
                var pushChannel = client.Channels.Get(channelName).Push;

                await pushChannel.SubscribeClient();

                (await taskAwaiter).Should().BeTrue("Didn't validate function");
            }

            public GeneralTests(ITestOutputHelper output)
                : base(output)
            {
            }
        }
    }
}
