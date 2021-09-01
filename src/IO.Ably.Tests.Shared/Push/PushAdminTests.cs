using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Push;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Push
{
    [Trait("spec", "RSH1")]
    public class PushAdminTests
    {
        public class GeneralTests : MockHttpRestSpecs
        {
            [Fact]
            [Trait("spec", "RSH6")]
            [Trait("spec", "RSH6a")]
            public void WhenLocalDeviceHasDeviceIdentityToken_ShouldAddHeaderToRequestWithCorrectValue()
            {
                var request = new AblyRequest("/", HttpMethod.Get);
                var localDevice = new LocalDevice() { DeviceIdentityToken = "test" };

                var rest = GetRestClient();
                rest.Push.Admin.AddDeviceAuthenticationToRequest(request, localDevice);

                request.Headers.Should().ContainKey(Defaults.DeviceIdentityTokenHeader).WhichValue.Should().Be("test");
            }

            [Fact]
            [Trait("spec", "RSH6")]
            [Trait("spec", "RSH6b")]
            public void WhenLocalDeviceHasDeviceSecret_ShouldAddHeaderToRequestWithCorrectValue()
            {
                var request = new AblyRequest("/", HttpMethod.Get);
                var localDevice = new LocalDevice() { DeviceSecret = "test" };

                var rest = GetRestClient();
                rest.Push.Admin.AddDeviceAuthenticationToRequest(request, localDevice);

                request.Headers.Should().ContainKey(Defaults.DeviceSecretHeader).WhichValue.Should().Be("test");
            }

            [Fact]
            [Trait("spec", "RSH6")]
            public void WhenLocalDeviceHasBothDeviceIdentityTokenAndSecret_ShouldOnlyAddIdentityTokenHeader()
            {
                var request = new AblyRequest("/", HttpMethod.Get);
                var localDevice = new LocalDevice() { DeviceIdentityToken = "test", DeviceSecret = "secret" };

                var rest = GetRestClient();
                rest.Push.Admin.AddDeviceAuthenticationToRequest(request, localDevice);

                request.Headers.Should().ContainKey(Defaults.DeviceIdentityTokenHeader).WhichValue.Should().Be("test");
                request.Headers.Should().NotContainKey(Defaults.DeviceSecretHeader);
            }

            [Fact]
            [Trait("spec", "RSH6")]
            public void WhenLocalDevice_DoesNOT_HaveEitherDeviceIdentityTokenAndSecret_ShouldNotAddAnyHeaders()
            {
                var request = new AblyRequest("/", HttpMethod.Get);
                var localDevice = new LocalDevice();

                var rest = GetRestClient();
                rest.Push.Admin.AddDeviceAuthenticationToRequest(request, localDevice);

                request.Headers.Should().BeEmpty();
            }

            public GeneralTests(ITestOutputHelper output)
                : base(output)
            {
            }
        }

        public class PublishTests : MockHttpRestSpecs
        {
            [Fact]
            [Trait("spec", "RSH1a")]
            public async Task Publish_WithEmptyRecipientOrData_ShouldThrow()
            {
                var rest = GetRestClient();

                var recipientEx = await Assert.ThrowsAsync<AblyException>(() => rest.Push.Admin.PublishAsync(null, new JObject()));
                recipientEx.ErrorInfo.Code.Should().Be(ErrorCodes.BadRequest);

                var dataEx = await Assert.ThrowsAsync<AblyException>(() => rest.Push.Admin.PublishAsync(new JObject(), null));
                dataEx.ErrorInfo.Code.Should().Be(ErrorCodes.BadRequest);
            }

            [Fact]
            [Trait("spec", "RSH1a")]
            public async Task Publish_ShouldMakeRequest_ToCorrectUrl()
            {
                bool requestCompleted = false;
                var recipient = JObject.FromObject(new { transportType = "fcm", registrationToken = "token" });
                var payload = JObject.FromObject(new { data = "data" });

                var rest = GetRestClient(request =>
                {
                    request.Url.Should().Be("/push/publish");
                    var data = (JObject)request.PostData;
                    data.Should().NotBeNull();
                    // Recipient should be set in the recipient property
                    ((JObject)data["recipient"]).Should().BeSameAs(recipient);

                    var expected = payload.DeepClone();
                    expected["recipient"] = recipient;
                    data.Should().BeEquivalentTo(expected);

                    requestCompleted = true;

                    return Task.FromResult(new AblyResponse() { StatusCode = HttpStatusCode.Accepted });
                });

                Func<Task> publishAction = async () => await rest.Push.Admin.PublishAsync(recipient, payload);

                await publishAction.Should().NotThrowAsync();

                requestCompleted.Should().BeTrue();
            }

            public PublishTests(ITestOutputHelper output)
                : base(output)
            {
            }
        }

        [Trait("spec", "RSH1b")]
        public class DeviceRegistrationTests : MockHttpRestSpecs
        {
            [Fact]
            [Trait("spec", "RSH1b1")]
            public async Task Get_ShouldThrowIfNoOrEmptyDeviceIdIsProvided()
            {
                var rest = GetRestClient();

                Func<string, Task> func = (deviceId) => rest.Push.Admin.DeviceRegistrations.GetAsync(deviceId);

                Func<Task> withEmptyDeviceId = () => func(string.Empty);
                Func<Task> withNullDeviceId = () => func(null);

                await withEmptyDeviceId.Should().ThrowAsync<AblyException>();
                await withNullDeviceId.Should().ThrowAsync<AblyException>();
            }

            [Fact]
            [Trait("spec", "RSH1b1")]
            public async Task Get_ShouldCallTheCorrectEndpoint()
            {
                string urlCalled = null;
                var rest = GetRestClient(request =>
                {
                    urlCalled = request.Url;
                    return Task.FromResult(new AblyResponse
                    {
                        StatusCode = HttpStatusCode.OK,
                        TextResponse = JObject.FromObject(new LocalDevice()).ToString(),
                    });
                });

                var id = Guid.NewGuid().ToString("D");
                await rest.Push.Admin.DeviceRegistrations.GetAsync(id);

                urlCalled.Should().Be($"/push/deviceRegistrations/{id}");
            }

            [Fact]
            [Trait("spec", "RSH1b1")]
            public async Task Get_ShouldFailDeviceNotFound()
            {
                var rest = GetRestClient(request => Task.FromResult(new AblyResponse
                {
                    StatusCode = HttpStatusCode.NotFound,
                    TextResponse = string.Empty,
                }));

                var id = Guid.NewGuid().ToString("D");
                var result = await rest.Push.Admin.DeviceRegistrations.GetAsync(id);

                result.IsFailure.Should().BeTrue();
                result.Error.Code.Should().Be(ErrorCodes.NotFound);
            }

            [Fact]
            [Trait("spec", "RSH1b1")]
            public async Task Get_ShouldAddDeviceAuthHeadersWhenAvailable()
            {
                var rest = GetRestClient(request =>
                {
                    request.Headers.Should().ContainKey(Defaults.DeviceSecretHeader);

                    return Task.FromResult(new AblyResponse
                    {
                        StatusCode = HttpStatusCode.OK,
                        TextResponse = new LocalDevice().ToJson(),
                    });
                });

                rest.Device = new LocalDevice() { DeviceSecret = "secret" };

                var id = Guid.NewGuid().ToString("D");
                await rest.Push.Admin.DeviceRegistrations.GetAsync(id);
            }

            [Fact]
            [Trait("spec", "RSH1b2")]
            public async Task List_ShouldCallCorrectUrlAndQueryParameters()
            {
                Func<ListDeviceDetailsRequest, Task<AblyRequest>> listDevices =
                    async query =>
                    {
                        AblyRequest currentRequest = null;
                        var client = GetRestClient(request =>
                        {
                            currentRequest = request;
                            return Task.FromResult(new AblyResponse() { StatusCode = HttpStatusCode.OK });
                        });

                        await client.Push.Admin.DeviceRegistrations.List(query);

                        return currentRequest;
                    };

                var emptyFilterRequest = await listDevices(ListDeviceDetailsRequest.Empty(100));
                emptyFilterRequest.Url.Should().Be("/push/deviceRegistrations");
                emptyFilterRequest.QueryParameters.Should().HaveCount(1);
                emptyFilterRequest.QueryParameters.Should().ContainKey("limit");

                var deviceIdRequest = await listDevices(ListDeviceDetailsRequest.WithDeviceId("123"));
                deviceIdRequest.Url.Should().Be("/push/deviceRegistrations");
                deviceIdRequest.QueryParameters.Should().ContainKey("deviceId").WhichValue.Should().Be("123");

                var clientIdRequest = await listDevices(ListDeviceDetailsRequest.WithClientId("234"));
                clientIdRequest.Url.Should().Be("/push/deviceRegistrations");
                clientIdRequest.QueryParameters.Should().ContainKey("clientId").WhichValue.Should().Be("234");
            }

            [Fact]
            [Trait("spec", "RSH1b3")]
            public async Task Save_ShouldCallTheCorrectUrlWithTheCorrectPayload()
            {
                AblyRequest executedRequest = null;
                var restClient = GetRestClient(request =>
                {
                    executedRequest = request;
                    return Task.FromResult(new AblyResponse() { TextResponse = string.Empty });
                });

                var deviceDetails = new LocalDevice() { Id = "123" };

                _ = await restClient.Push.Admin.DeviceRegistrations.SaveAsync(deviceDetails);

                executedRequest.Url.Should().Be($"/push/deviceRegistrations/{deviceDetails.Id}");
                executedRequest.PostData.Should().BeSameAs(deviceDetails);
            }

            [Fact]
            [Trait("spec", "RSH1b3")]
            public async Task Save_ShouldAddDeviceAuthenticationIfDeviceIdMatchesLocalDeviceSaved()
            {
                AblyRequest executedRequest = null;
                var restClient = GetRestClient(request =>
                {
                    executedRequest = request;
                    return Task.FromResult(new AblyResponse() { TextResponse = string.Empty });
                });

                var deviceDetails = new LocalDevice() { Id = "123" };
                restClient.Device = new LocalDevice() { Id = "123", DeviceIdentityToken = "token" };

                _ = await restClient.Push.Admin.DeviceRegistrations.SaveAsync(deviceDetails);

                executedRequest.Headers.Should().ContainKey(Defaults.DeviceIdentityTokenHeader);
            }

            [Fact]
            [Trait("spec", "RSH1b3")]
            public async Task Save_ShouldNotAddDeviceAuthenticationWhenDeviceIdDoesNotMatchLocalDeviceSaved()
            {
                AblyRequest executedRequest = null;
                var restClient = GetRestClient(request =>
                {
                    executedRequest = request;
                    return Task.FromResult(new AblyResponse() { TextResponse = string.Empty });
                });

                var deviceDetails = new LocalDevice() { Id = "123" };
                restClient.Device = new LocalDevice() { Id = "456", DeviceIdentityToken = "token" };

                _ = await restClient.Push.Admin.DeviceRegistrations.SaveAsync(deviceDetails);

                executedRequest.Headers.Should().NotContainKey(Defaults.DeviceIdentityTokenHeader);
            }

            [Fact]
            [Trait("spec", "RSH1b3")]
            public async Task Save_ShouldThrowWithInvalidDeviceDetails()
            {
                var restClient = GetRestClient();

                Func<DeviceDetails, Task> callSave = (deviceDetails) => restClient.Push.Admin.DeviceRegistrations.SaveAsync(deviceDetails);

                Func<Task> withNullDeviceDetails = () => callSave(null);
                Func<Task> withDeviceDetailsWithoutId = () => callSave(new DeviceDetails());

                (await withNullDeviceDetails.Should().ThrowAsync<AblyException>()).Which.ErrorInfo.Code.Should()
                    .Be(ErrorCodes.BadRequest);

                (await withDeviceDetailsWithoutId.Should().ThrowAsync<AblyException>()).Which.ErrorInfo.Code.Should()
                    .Be(ErrorCodes.BadRequest);
            }

            [Fact]
            [Trait("spec", "RSH1b4")]
            public async Task Delete_ShouldUseTheCorrectUrl()
            {
                AblyRequest currentRequest = null;
                var client = GetRestClient(request =>
                {
                    currentRequest = request;
                    return Task.FromResult(new AblyResponse() { StatusCode = HttpStatusCode.OK });
                });

                var deviceId = "123";
                await client.Push.Admin.DeviceRegistrations.RemoveAsync(deviceId);

                currentRequest.Url.Should().Be($"/push/deviceRegistrations/{deviceId}");
                currentRequest.Method.Should().Be(HttpMethod.Delete);
            }

            [Fact]
            [Trait("spec", "RSH1b4")]
            public async Task Delete_ThrowWhenThereIsNoDeviceIdPassed()
            {
                var client = GetRestClient();

                Func<string, Task> callDelete = (deviceId) => client.Push.Admin.DeviceRegistrations.RemoveAsync(deviceId);

                Func<Task> withNullDeviceId = () => callDelete(null);
                Func<Task> withEmptyDeviceId = () => callDelete(string.Empty);

                (await withEmptyDeviceId.Should().ThrowAsync<AblyException>()).Which.ErrorInfo.Code.Should()
                    .Be(ErrorCodes.BadRequest);

                (await withNullDeviceId.Should().ThrowAsync<AblyException>()).Which.ErrorInfo.Code.Should()
                    .Be(ErrorCodes.BadRequest);
            }

            [Fact]
            [Trait("spec", "RSH1b5")]
            public async Task RemoveWhere_CallsTheCorrectUrlWithFiltersInQuery()
            {
                AblyRequest currentRequest = null;
                var client = GetRestClient(request =>
                {
                    currentRequest = request;
                    return Task.FromResult(new AblyResponse() { StatusCode = HttpStatusCode.OK });
                });

                client.Push.Admin.DeviceRegistrations.RemoveWhereAsync(
                    new Dictionary<string, string>
                    {
                        { "deviceId", "test" },
                        { "random", "boo" },
                    });

                currentRequest.Url.Should().Be("/push/deviceRegistrations");
                currentRequest.QueryParameters.Should().ContainKey("deviceId").WhichValue.Should().Be("test");
                currentRequest.QueryParameters.Should().ContainKey("random").WhichValue.Should().Be("boo");
            }

            [Fact]
            [Trait("spec", "RSH1b5")]
            public async Task RemoveWhere_PassesDeviceAuthHeaderIfDeviceIdFilterMatchesCurrentDevice()
            {
                AblyRequest currentRequest = null;
                var client = GetRestClient(request =>
                {
                    currentRequest = request;
                    return Task.FromResult(new AblyResponse() { StatusCode = HttpStatusCode.OK });
                });

                client.Device = new LocalDevice() { Id = "123", DeviceIdentityToken = "token" };
                client.Push.Admin.DeviceRegistrations.RemoveWhereAsync(
                    new Dictionary<string, string>
                    {
                        { "deviceId", "123" },
                    });

                currentRequest.Headers.Should().ContainKey(Defaults.DeviceIdentityTokenHeader).WhichValue.Should().Be("token");
            }

            public DeviceRegistrationTests(ITestOutputHelper output)
                : base(output)
            {
            }
        }

        [Trait("spec", "RSH1c")]
        public class ChannelSubscriptionsTests : MockHttpRestSpecs
        {
            [Fact]
            [Trait("spec", "RSH1c1")]
            public async Task List_ShouldCallTheCorrectUrl()
            {
                AblyRequest request = null;
                var rest = GetRestClient(r =>
                {
                    request = r;
                    return Task.FromResult(new AblyResponse() { TextResponse = string.Empty });
                });

                await rest.Push.Admin.ChannelSubscriptions.ListAsync(ListSubscriptionsRequest.Empty());

                request.Url.Should().Be("/push/channelSubscriptions");
                request.Method.Should().Be(HttpMethod.Get);
            }

            [Fact]
            [Trait("spec", "RSH1c1")]
            public async Task List_ShouldPassTheCorrectFilters()
            {
                Func<ListSubscriptionsRequest, Task<AblyRequest>> callList = async filter =>
                {
                    AblyRequest request = null;
                    var rest = GetRestClient(r =>
                    {
                        request = r;
                        return Task.FromResult(new AblyResponse() { TextResponse = string.Empty });
                    });
                    await rest.Push.Admin.ChannelSubscriptions.ListAsync(filter);
                    return request;
                };

                var emptyFilterRequest = await callList(ListSubscriptionsRequest.Empty(100));
                emptyFilterRequest.QueryParameters.Should().ContainKey("limit").WhichValue.Should().Be("100");

                var channelDeviceIdRequest =
                    await callList(ListSubscriptionsRequest.WithDeviceId("test-channel", "device123"));

                channelDeviceIdRequest.QueryParameters.Should().ContainKey("channel")
                    .WhichValue.Should().Be("test-channel");
                channelDeviceIdRequest.QueryParameters.Should().ContainKey("deviceId")
                    .WhichValue.Should().Be("device123");

                var channelClientIdRequest =
                    await callList(ListSubscriptionsRequest.WithClientId("test-channel", "clientId123"));

                channelClientIdRequest.QueryParameters.Should().ContainKey("channel")
                    .WhichValue.Should().Be("test-channel");
                channelClientIdRequest.QueryParameters.Should().ContainKey("clientId")
                    .WhichValue.Should().Be("clientId123");
            }

            [Fact]
            [Trait("spec", "RSH1c2")]
            public async Task ListChannels_ShouldCallsTheCorrectUrl()
            {
                Func<PaginatedRequestParams, Task<AblyRequest>> callListChannels = async filter =>
                {
                    AblyRequest request = null;
                    var rest = GetRestClient(r =>
                    {
                        request = r;
                        return Task.FromResult(new AblyResponse() { TextResponse = string.Empty });
                    });
                    await rest.Push.Admin.ChannelSubscriptions.ListChannelsAsync(filter);
                    return request;
                };

                var request = await callListChannels(PaginatedRequestParams.Empty);

                request.Url.Should().Be("/push/channels");

                var limitRequest = await callListChannels(new PaginatedRequestParams { Limit = 150 });
                limitRequest.QueryParameters.Should().ContainKey("limit").WhichValue.Should().Be("150");
            }

            [Fact]
            [Trait("spec", "RSH1c3")]
            public async Task Save_ShouldCallsTheCorrectUrlAndHaveTheCorrectBody()
            {
                Func<PushChannelSubscription, Task<AblyRequest>> callSave = async subscription =>
                {
                    AblyRequest request = null;
                    var rest = GetRestClient(r =>
                    {
                        request = r;
                        return Task.FromResult(new AblyResponse() { TextResponse = string.Empty });
                    });

                    await rest.Push.Admin.ChannelSubscriptions.SaveAsync(subscription);
                    return request;
                };

                var sub = PushChannelSubscription.ForDevice("test");
                var request = await callSave(sub);

                request.Url.Should().Be("/push/channelSubscriptions");
                request.Method.Should().Be(HttpMethod.Post);
                request.PostData.Should().BeSameAs(sub);
            }

            [Fact]
            [Trait("spec", "RSH1c3")]
            public async Task Save_ShouldValidateSubscriptionBeforeSendingItToTheServer()
            {
                Func<PushChannelSubscription, Task<AblyRequest>> callSave = async subscription =>
                {
                    AblyRequest request = null;
                    var rest = GetRestClient(r =>
                    {
                        request = r;
                        return Task.FromResult(new AblyResponse() { TextResponse = string.Empty });
                    });

                    await rest.Push.Admin.ChannelSubscriptions.SaveAsync(subscription);
                    return request;
                };

                Func<Task> nullSubscription = () => callSave(null);
                Func<Task> withEmptyChannel = () => callSave(PushChannelSubscription.ForDevice(string.Empty));

                (await nullSubscription.Should().ThrowAsync<AblyException>()).Which.ErrorInfo.Code.Should()
                    .Be(ErrorCodes.BadRequest);

                (await withEmptyChannel.Should().ThrowAsync<AblyException>()).Which.ErrorInfo.Code.Should()
                    .Be(ErrorCodes.BadRequest);
            }

            [Fact]
            [Trait("spec", "RSH1c3")]
            public async Task Save_ShouldUseDeviceAuthIfDeviceIdMatches()
            {
                AblyRequest request = null;
                var rest = GetRestClient(r =>
                {
                    request = r;
                    return Task.FromResult(new AblyResponse() { TextResponse = string.Empty });
                });
                rest.Device = new LocalDevice() { Id = "123", DeviceIdentityToken = "token" };

                var sub = PushChannelSubscription.ForDevice("test", "123");
                await rest.Push.Admin.ChannelSubscriptions.SaveAsync(sub);

                request.Headers.Should().ContainKey(Defaults.DeviceIdentityTokenHeader).WhichValue.Should().Be("token");
            }

            [Fact]
            [Trait("spec", "RSH1c4")]
            public async Task Remove_ShouldCallTheCorrectUrl()
            {
                Func<PushChannelSubscription, Task<AblyRequest>> callRemove = async subscription =>
                {
                    AblyRequest request = null;
                    var rest = GetRestClient(r =>
                    {
                        request = r;
                        return Task.FromResult(new AblyResponse() { TextResponse = string.Empty });
                    });

                    await rest.Push.Admin.ChannelSubscriptions.RemoveAsync(subscription);
                    return request;
                };

                var request = await callRemove(PushChannelSubscription.ForDevice("channel", "device"));
                request.Url.Should().Be("/push/channelSubscriptions");
                request.Method.Should().Be(HttpMethod.Delete);

                request.QueryParameters.Should().ContainKey("channel").WhichValue.Should().Be("channel");
                request.QueryParameters.Should().ContainKey("deviceId").WhichValue.Should().Be("device");

                var requestWithClientId = await callRemove(PushChannelSubscription.ForClientId("channel", "123"));

                requestWithClientId.QueryParameters.Should().ContainKey("channel").WhichValue.Should().Be("channel");
                requestWithClientId.QueryParameters.Should().ContainKey("clientId").WhichValue.Should().Be("123");
            }

            [Fact]
            [Trait("spec", "RSH1c5")]
            public async Task RemoveWhere_ShouldCallTheCorrectUrl()
            {
                Func<IDictionary<string, string>, Task<AblyRequest>> callRemoveWhere = async whereParams =>
                {
                    AblyRequest request = null;
                    var rest = GetRestClient(r =>
                    {
                        request = r;
                        return Task.FromResult(new AblyResponse() { TextResponse = string.Empty });
                    });

                    await rest.Push.Admin.ChannelSubscriptions.RemoveWhereAsync(whereParams);
                    return request;
                };

                var request = await callRemoveWhere(new Dictionary<string, string>());
                request.Url.Should().Be("/push/channelSubscriptions");
                request.Method.Should().Be(HttpMethod.Delete);
                request.QueryParameters.Should().BeEmpty();

                var requestWithChannelAndDeviceId = await callRemoveWhere(new Dictionary<string, string>() { { "channel", "test" }, { "deviceId", "best" } });

                requestWithChannelAndDeviceId.QueryParameters.Should().ContainKey("channel").WhichValue.Should().Be("test");
                requestWithChannelAndDeviceId.QueryParameters.Should().ContainKey("deviceId").WhichValue.Should().Be("best");

                var requestWithRandomParameter = await callRemoveWhere(new Dictionary<string, string>() { { "random", "value" } });

                requestWithRandomParameter.QueryParameters.Should().ContainKey("random").WhichValue.Should().Be("value");
            }

            public ChannelSubscriptionsTests(ITestOutputHelper output)
                : base(output)
            {
            }
        }
    }
}
