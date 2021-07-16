using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Push;
using IO.Ably.Tests.Infrastructure;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.DotNetCore20.Push
{
    public static class PushAdminSandboxTests
    {
        [Trait("type", "integration")]
        public class PublishTests : SandboxSpecs
        {
            [Theory]
            [ProtocolData]
            [Trait("spec", "RSH1a")]
            public async Task ShouldSuccessfullyPublishAPayload(Protocol protocol)
            {
                // Arrange
                var client = await GetRealtimeClient(protocol);
                var channelName = "pushenabled:test".AddRandomSuffix();

                var channel = client.Channels.Get(channelName);
                await channel.AttachAsync();

                var pushPayload = JObject.FromObject(new { notification = new { title = "test", body = "message body" }, data = new { foo = "bar" }, });
                var pushRecipient = JObject.FromObject(new
                {
                    transportType = "ablyChannel",
                    channel = channelName,
                    ablyKey = client.Options.Key,
                    ablyUrl = "https://" + client.Options.FullRestHost(),
                });

                var awaiter = new TaskCompletionAwaiter();
                channel.Subscribe(message =>
                {
                    // Assert
                    message.Name.Should().Be("__ably_push__");
                    var payload = JObject.Parse((string)message.Data);
                    payload["data"].Should().BeEquivalentTo(pushPayload["data"]);
                    ((string)payload["notification"]["title"]).Should().Be("test");
                    ((string)payload["notification"]["body"]).Should().Be("message body");

                    awaiter.SetCompleted();
                });

                // Act
                await client.Push.Admin.PublishAsync(pushRecipient, pushPayload);

                // Wait for 10 seconds for awaiter.SetCompleted() and make sure it was called.
                (await awaiter.Task).Should().BeTrue();
            }

            public PublishTests(AblySandboxFixture fixture, ITestOutputHelper output)
                : base(fixture, output)
            {
            }
        }

        [Trait("type", "integration")]
        public class DeviceRegistrationsTests : SandboxSpecs
        {
            [Theory]
            [ProtocolData]
            [Trait("spec", "RSH1")]
            public async Task ShouldSuccessfullyRegisterDevice(Protocol protocol)
            {
                // Arrange
                var client = await GetRestClient(protocol, options => options.PushAdminFullWait = true);

                var device = GetTestLocalDevice(client);

                Func<Task> callRegister = async () =>
                {
                    await client.Push.Admin.RegisterDevice(device);
                };

                await callRegister.Should().NotThrowAsync<AblyException>();
            }

            [Theory]
            [ProtocolData]
            [Trait("spec", "RSH1b3")]
            public async Task ShouldSuccessfullySaveDeviceRegistration(Protocol protocol)
            {
                // Arrange
                var client = await GetRestClient(protocol, options => options.PushAdminFullWait = true);

                var device = GetTestLocalDevice(client);

                Func<Task> callSave = async () =>
                {
                    var savedDevice = await client.Push.Admin.DeviceRegistrations.SaveAsync(device);

                    savedDevice.Metadata = JObject.FromObject(new { tag = "test-tag" });
                    savedDevice.Push.State = null; // Clear state as we don't care about it.

                    var updatedDevice = await client.Push.Admin.DeviceRegistrations.SaveAsync(savedDevice);
                    updatedDevice.Metadata.Should().BeEquivalentTo(savedDevice.Metadata);
                };

                await callSave.Should().NotThrowAsync<AblyException>();
            }

            [Theory]
            [ProtocolData]
            [Trait("spec", "RSH1b4")]
            public async Task ShouldSuccessfullySaveAndDeleteDeviceRegistration(Protocol protocol)
            {
                // Arrange
                var client = await GetRestClient(protocol, options => options.PushAdminFullWait = true);

                var device = GetTestLocalDevice(client);

                Func<Task> callSaveAndDelete = async () =>
                {
                    var savedDevice = await client.Push.Admin.DeviceRegistrations.SaveAsync(device);
                    await client.Push.Admin.DeviceRegistrations.RemoveAsync(savedDevice);
                };

                await callSaveAndDelete.Should().NotThrowAsync<AblyException>();
            }

            [Theory]
            [ProtocolData]
            [Trait("spec", "RSH1b4")]
            public async Task ShouldSuccessfullyDeleteNotExistentDeviceRegistration(Protocol protocol)
            {
                // Arrange
                var client = await GetRestClient(protocol, options => options.PushAdminFullWait = true);

                Func<Task> callSaveAndDelete = async () =>
                {
                    await client.Push.Admin.DeviceRegistrations.RemoveAsync("1234");
                };

                await callSaveAndDelete.Should().NotThrowAsync<AblyException>();
            }

            [Theory]
            [ProtocolData]
            [Trait("spec", "RSH1b5")]
            public async Task ShouldSuccessfullyDeleteDeviceRegistrationWithFilterByClientId(Protocol protocol)
            {
                // Arrange
                var client = await GetRestClient(protocol, options => options.PushAdminFullWait = true);

                var device = GetTestLocalDevice(client);

                Func<Task> callSaveAndDelete = async () =>
                {
                    await client.Push.Admin.DeviceRegistrations.SaveAsync(device);
                    await client.Push.Admin.DeviceRegistrations.RemoveWhereAsync(new Dictionary<string, string> { { "clientId", device.ClientId } });
                };

                await callSaveAndDelete.Should().NotThrowAsync<AblyException>();
            }

            [Theory]
            [ProtocolData]
            [Trait("spec", "RSH1b5")]
            public async Task ShouldSuccessfullyDeleteDeviceRegistrationWithFilterByDeviceId(Protocol protocol)
            {
                // Arrange
                var client = await GetRestClient(protocol, options => options.PushAdminFullWait = true);

                var device = GetTestLocalDevice(client);

                Func<Task> callSaveAndDelete = async () =>
                {
                    await client.Push.Admin.DeviceRegistrations.SaveAsync(device);
                    await client.Push.Admin.DeviceRegistrations.RemoveWhereAsync(new Dictionary<string, string> { { "deviceId", device.Id } });
                };

                await callSaveAndDelete.Should().NotThrowAsync<AblyException>();
            }

            [Theory]
            [ProtocolData]
            [Trait("spec", "RSH1b5")]
            public async Task ShouldSuccessfullyDeleteDeviceRegistrationWithOutMatchingFilter(Protocol protocol)
            {
                // Arrange
                var client = await GetRestClient(protocol, options => options.PushAdminFullWait = true);

                Func<Task> callRemoveWithNoFilter = async () =>
                {
                    await client.Push.Admin.DeviceRegistrations.RemoveWhereAsync(new Dictionary<string, string>() { { "deviceId", "test" } });
                };

                await callRemoveWithNoFilter.Should().NotThrowAsync<AblyException>();
            }

            private static LocalDevice GetTestLocalDevice(AblyRest client)
            {
                var device = LocalDevice.Create("123");
                device.FormFactor = "phone";
                device.Platform = "android";
                device.Push.Recipient = JObject.FromObject(new
                {
                    transportType = "ablyChannel",
                    channel = "pushenabled:test",
                    ablyKey = client.Options.Key,
                    ablyUrl = "https://" + client.Options.FullRestHost(),
                });
                return device;
            }

            public DeviceRegistrationsTests(AblySandboxFixture fixture, ITestOutputHelper output)
                : base(fixture, output)
            {
            }
        }
    }
}
