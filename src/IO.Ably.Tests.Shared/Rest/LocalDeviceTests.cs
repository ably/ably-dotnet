using FluentAssertions;
using IO.Ably.Tests.Push;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.DotNetCore20.Rest
{
    public class LocalDeviceTests : MockHttpRestSpecs
    {
        [Fact]
        [Trait("spec", "RSH8")]
        public void WhenPlatformsSupportsPushNotifications_ShouldBeAbleToRetrieveLocalDeviceFromRestClient()
        {
            var mobileDevice = new FakeMobileDevice();

            var rest = GetRestClient(mobileDevice: mobileDevice);

            rest.Device.Should().NotBeNull();
        }

        [Fact]
        [Trait("spec", "RSH8")]
        public void WhenPlatformsSupportsPushNotifications_ShouldBeAbleToRetrieveLocalDeviceFromRealtimeClient()
        {
            var mobileDevice = new FakeMobileDevice();

            var options = new ClientOptions(ValidKey)
            {
                AutoConnect = false
            };

            var realtime = new AblyRealtime(options, mobileDevice: mobileDevice);

            realtime.Device.Should().NotBeNull();
        }

        public LocalDeviceTests(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
