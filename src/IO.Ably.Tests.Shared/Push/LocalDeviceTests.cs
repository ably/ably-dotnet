using Xunit;
using System.Linq;
using FluentAssertions;
using IO.Ably.Push;

namespace IO.Ably.Tests.DotNetCore20.Push
{
    public class LocalDeviceTests
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
    }
}
