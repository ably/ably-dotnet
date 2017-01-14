using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Trait("spec", "RTN2")]
    public class ConnectionParameterSpecs : ConnectionSpecsBase
    {
        [Fact]
        [Trait("spec", "RTN2")]
        public void ShouldUseDefaultRealtimeHost()
        {
            var client = GetClientWithFakeTransport();
            LastCreatedTransport.Parameters.Host.Should().Be(Defaults.RealtimeHost);
        }

        [Theory]
        [InlineData(true, "msgpack")]
        [InlineData(false, "json")]
        [Trait("spec", "RTN2a")]
        public void WithUseBinaryEncoding_ShouldSetTransportFormatProperty(bool useBinary, string format)
        {
            var client = GetClientWithFakeTransport(opts => opts.UseBinaryProtocol = useBinary);
            LastCreatedTransport.Parameters.UseBinaryProtocol.Should().Be(useBinary);
            LastCreatedTransport.Parameters.GetParams().Should().ContainKey("format").WhichValue.Should().Be(format);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [Trait("spec", "RTN2b")]
        public void WithEchoInClientOptions_ShouldSetTransportEchoCorrectly(bool echo)
        {
            var client = GetClientWithFakeTransport(opts => opts.EchoMessages = echo);

            LastCreatedTransport.Parameters.EchoMessages.Should().Be(echo);
            LastCreatedTransport.Parameters.GetParams()
                .Should().ContainKey("echo")
                .WhichValue.Should().Be(echo.ToString().ToLower());
        }



        [Fact]
        [Trait("spec", "RTN2d")]
        public void WithClientId_ShouldSetTransportClientIdCorrectly()
        {
            var clientId = "12345";
            var client = GetClientWithFakeTransport(opts =>
            {
                opts.ClientId = clientId;
                opts.Token = "123";

            });

            LastCreatedTransport.Parameters.ClientId.Should().Be(clientId);
            LastCreatedTransport.Parameters.GetParams()
                .Should().ContainKey("clientId")
                .WhichValue.Should().Be(clientId);
        }

        [Fact]
        [Trait("spec", "RTN2d")]
        public void WithoutClientId_ShouldNotSetClientIdParameterOnTransport()
        {
            var client = GetClientWithFakeTransport();

            LastCreatedTransport.Parameters.ClientId.Should().BeNullOrEmpty();
            LastCreatedTransport.Parameters.GetParams().Should().NotContainKey("clientId");
        }

        [Fact]
        [Trait("spec", "RTN2e")]
        public void WithBasicAuth_ShouldSetTransportKeyParameter()
        {
            var client = GetClientWithFakeTransport();
            LastCreatedTransport.Parameters.AuthValue.Should().Be(client.Options.Key);
            LastCreatedTransport.Parameters.GetParams().
                Should().ContainKey("key")
                .WhichValue.Should().Be(client.Options.Key);
        }

        [Fact]
        [Trait("spec", "RTN2e")]
        [Trait("spec", "RSA3c")]
        public void WithTokenAuth_ShouldSetTransportAccessTokeParameter()
        {
            var clientId = "123";
            var tokenString = "token";
            var client = GetClientWithFakeTransport(opts =>
            {
                opts.Key = "";
                opts.ClientId = clientId;
                opts.Token = tokenString;

            });

            LastCreatedTransport.Parameters.AuthValue.Should().Be(tokenString);
            LastCreatedTransport.Parameters.GetParams()
                .Should().ContainKey("accessToken")
                .WhichValue.Should().Be(tokenString);
        }

        [Fact]
        [Trait("spec", "RTN2f")]
        public void ShouldSetTransportVersionParameterTov08()
        {
            var client = GetClientWithFakeTransport();

            LastCreatedTransport.Parameters.GetParams()
                .Should().ContainKey("v")
                .WhichValue.Should().Be("0.8");
        }

        [Trait("spec", "RTN2g")]
        public void ShouldSetTransportLibVersionParamater()
        {
            var client = GetClientWithFakeTransport();

            LastCreatedTransport.Parameters.GetParams()
                .Should().ContainKey("lib")
                .WhichValue.Should().Be("dotnet-0.8.4");
        }

        public ConnectionParameterSpecs(ITestOutputHelper output) : base(output)
        {

        }
    }
}