using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Push;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.DotNetCore20.Push
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

                request.Headers.Should().ContainKey("X-Ably-DeviceIdentityToken").WhichValue.Should().Be("test");
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

                request.Headers.Should().ContainKey("X-Ably-DeviceSecret").WhichValue.Should().Be("test");
            }

            [Fact]
            [Trait("spec", "RSH6")]
            public void WhenLocalDeviceHasBothDeviceIdentityTokenAndSecret_ShouldOnlyAddIdentityTokenHeader()
            {
                var request = new AblyRequest("/", HttpMethod.Get);
                var localDevice = new LocalDevice() { DeviceIdentityToken = "test", DeviceSecret = "secret" };

                var rest = GetRestClient();
                rest.Push.Admin.AddDeviceAuthenticationToRequest(request, localDevice);

                request.Headers.Should().ContainKey("X-Ably-DeviceIdentityToken").WhichValue.Should().Be("test");
                request.Headers.Should().NotContainKey("X-Ably-DeviceSecret");
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
                    request.Headers.Should().ContainKey("X-Ably-DeviceSecret");

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

            public DeviceRegistrationTests(ITestOutputHelper output)
                : base(output)
            {
            }
        }
    }
}
