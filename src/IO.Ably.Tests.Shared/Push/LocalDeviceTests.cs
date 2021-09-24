using System;
using Xunit;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Push;
using IO.Ably.Realtime;
using IO.Ably.Tests.Realtime;
using IO.Ably.Types;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Push
{
    public class LocalDeviceTests : MockHttpRestSpecs, IDisposable
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

        [Fact]
        [Trait("spec", "RSH8")]
        public void WhenPlatformsDoesNotSupportPushNotifications_DeviceShouldBeNull()
        {
            // Realtime check
            var realtime = new AblyRealtime(new ClientOptions(ValidKey)
            {
                AutoConnect = false
            });
            realtime.Device.Should().BeNull();

            // Rest check
            var rest = new AblyRest(ValidKey);
            rest.MobileDevice.Should().BeNull();
        }

        [Fact]
        [Trait("spec", "RSH8a")]
        public void LocalDevice_IsOnlyInitialisedOnce()
        {
            var restClient = GetRestClient(mobileDevice: new FakeMobileDevice());
            var restClient2 = GetRestClient(mobileDevice: new FakeMobileDevice());

            restClient.Device.Should().BeSameAs(restClient2.Device);
        }

        [Fact]
        [Trait("spec", "RSH8a")]
        [Trait("spec", "RSH8b")]
        public void LocalDevice_WhenInitialised_ShouldHave_CorrectProperties_set()
        {
            var restClient = GetRestClient(mobileDevice: new FakeMobileDevice());

            var device = restClient.Device;

            device.Id.Should().NotBeEmpty();
            device.DeviceSecret.Should().NotBeEmpty();
            device.Platform.Should().NotBeEmpty();
            device.FormFactor.Should().NotBeEmpty();
        }

        [Fact]
        [Trait("spec", "RSH8a")]
        [Trait("spec", "RSH8b")]
        public void LocalDevice_WhenRestClientContainsClientId_ShouldHaveTheSameClientId()
        {
            const string optionsClientId = "123";
            var restClient = GetRestClient(setOptionsAction: options => options.ClientId = optionsClientId, mobileDevice: new FakeMobileDevice());
            var device = restClient.Device;

            device.ClientId.Should().Be(optionsClientId);
        }

        [Fact]
        [Trait("spec", "RSA7b2")]
        [Trait("spec", "RSH8d")]
        public async Task WithoutClientId_WhenAuthorizedWithTokenParamsWithClientId_ShouldUpdateLocalDeviceClientId()
        {
            const string newClientId = "123";
            var mobileDevice = new FakeMobileDevice();
            var ably = GetRestClient(
                handleRequestFunc: async request => new AblyResponse() { TextResponse = new TokenDetails("token").ToJson() },
                mobileDevice: mobileDevice);

            var localDevice = ably.Device;
            localDevice.ClientId.Should().BeNull();

            _ = await ably.Auth.AuthorizeAsync(new TokenParams { ClientId = newClientId });

            localDevice.ClientId.Should().Be(newClientId);
            mobileDevice.GetPreference(PersistKeys.Device.ClientId, PersistKeys.Device.SharedName).Should().Be(newClientId);
        }

        [Fact]
        [Trait("spec", "RSA7b3")]
        [Trait("spec", "RSH8d")]
        public async Task WhenConnectedMessageContainsClientId_AuthClientIdShouldBeTheSame()
        {
            // Arrange
            var options = new ClientOptions(ValidKey) { TransportFactory = new FakeTransportFactory(), SkipInternetCheck = true };
            var mobileDevice = new FakeMobileDevice();
            var realtime = new AblyRealtime(options, mobileDevice: mobileDevice);
            const string newClientId = "testId";

            var localDevice = realtime.Device;
            localDevice.ClientId.Should().BeNull();

            // Act
            realtime.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected)
            {
                ConnectionDetails = new ConnectionDetails { ClientId = newClientId },
            });

            await realtime.WaitForState(ConnectionState.Connected);

            // Assert
            realtime.Auth.ClientId.Should().Be(newClientId);
            localDevice.ClientId.Should().Be(newClientId);
            mobileDevice.GetPreference(PersistKeys.Device.ClientId, PersistKeys.Device.SharedName).Should().Be(newClientId);
        }

        public LocalDeviceTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public void Dispose()
        {
            LocalDevice.Instance = null;
        }
    }
}
