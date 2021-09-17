using Xunit;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Push;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Push
{
    public class LocalDeviceTests : MockHttpRestSpecs
    {
        [Fact]
        [Trait("spec", "RSH3a2b")]
        public void Create_ShouldCreateRandomDeviceSecretsAndIds()
        {
            var secrets = Enumerable.Range(1, 100)
                                        .Select(x => LocalDevice.Create().DeviceSecret)
                                        .Distinct();

            var ids = Enumerable.Range(1, 100)
                                    .Select(x => LocalDevice.Create().Id)
                                    .Distinct();

            secrets.Should().HaveCount(100);
            ids.Should().HaveCount(100);
        }

        [Theory]
        [InlineData("id", "secret", true)]
        [InlineData("id", "", false)]
        [InlineData("", "secret", false)]
        public void IsCreated_ShouldBeTrueWhenBothIdAndSecretArePresent(string id, string secret, bool result)
        {
            var device = new LocalDevice()
            {
                Id = id,
                DeviceSecret = secret,
            };

            device.IsCreated.Should().Be(result);
        }

        [Fact]
        public async Task LoadPersistedLocalDevice_ShouldLoadAllSavedProperties()
        {
            var mobileDevice = new FakeMobileDevice();
            void SetSetting(string key, string value) => mobileDevice.SetPreference(key, value, PersistKeys.Device.SharedName);

            var stateMachine = new ActivationStateMachine(GetRestClient(mobileDevice: mobileDevice));

            const string deviceId = "deviceId";
            SetSetting(PersistKeys.Device.DeviceId, deviceId);
            const string clientId = "clientId";
            SetSetting(PersistKeys.Device.ClientId, clientId);
            const string deviceSecret = "secret";
            SetSetting(PersistKeys.Device.DeviceSecret, deviceSecret);
            const string identityToken = "token";
            SetSetting(PersistKeys.Device.DeviceToken, identityToken);
            const string tokenType = "fcm";
            SetSetting(PersistKeys.Device.TokenType, tokenType);
            const string token = "registration_token";
            SetSetting(PersistKeys.Device.Token, token);

            var loadResult = LocalDevice.LoadPersistedLocalDevice(mobileDevice, out var localDevice);
            loadResult.Should().BeTrue();
            localDevice.Platform.Should().Be(mobileDevice.DevicePlatform);
            localDevice.FormFactor.Should().Be(mobileDevice.FormFactor);

            localDevice.Id.Should().Be(deviceId);
            localDevice.ClientId.Should().Be(clientId);
            localDevice.DeviceSecret.Should().Be(deviceSecret);
            localDevice.DeviceIdentityToken.Should().Be(identityToken);
            localDevice.RegistrationToken.Type.Should().Be(tokenType);
            localDevice.RegistrationToken.Token.Should().Be(token);
        }

        [Fact]
        public async Task RestClient_LocalDevice_ShouldReturnSameInstanceForMultipleClients()
        {
            var mobileDevice = new FakeMobileDevice();
            var client1 = GetRestClient(mobileDevice: mobileDevice);
            var client2 = GetRestClient(mobileDevice: mobileDevice);

            client1.Device.Should().BeSameAs(client2.Device);
        }

        public LocalDeviceTests(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
