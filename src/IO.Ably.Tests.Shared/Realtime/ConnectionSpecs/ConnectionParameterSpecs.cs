using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Trait("spec", "RTN2")]
    public class ConnectionParameterSpecs : AblyRealtimeSpecs
    {
        [Fact]
        [Trait("spec", "RTN2")]
        public async Task ShouldUseDefaultRealtimeHost()
        {
            var client = await GetConnectedClient();
            LastCreatedTransport.Parameters.Host.Should().Be(Defaults.RealtimeHost);
        }

        [Theory]
        [InlineData(true, "msgpack")]
        [InlineData(false, "json")]
        [Trait("spec", "RTN2a")]
        public void WithUseBinaryEncoding_ShouldSetTransportFormatProperty(bool useBinary, string format)
        {
            if (!Defaults.MsgPackEnabled)
            {
                return;
            }

#pragma warning disable 162
            var client = GetClientWithFakeTransport(opts => opts.UseBinaryProtocol = useBinary);
            LastCreatedTransport.Parameters.UseBinaryProtocol.Should().Be(useBinary);
            LastCreatedTransport.Parameters.GetParams().Should().ContainKey("format").WhichValue.Should().Be(format);
#pragma warning restore 162
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [Trait("spec", "RTN2b")]
        public async Task WithEchoInClientOptions_ShouldSetTransportEchoCorrectly(bool echo)
        {
            var client = await GetConnectedClient(opts => opts.EchoMessages = echo);

            LastCreatedTransport.Parameters.EchoMessages.Should().Be(echo);
            LastCreatedTransport.Parameters.GetParams()
                .Should().ContainKey("echo")
                .WhichValue.Should().Be(echo.ToString().ToLower());
        }

        [Fact]
        [Trait("spec", "RTN2d")]
        public async Task WithClientId_ShouldSetTransportClientIdCorrectly()
        {
            var clientId = "12345";
            var client = await GetConnectedClient(opts =>
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
        public async Task WithoutClientId_ShouldNotSetClientIdParameterOnTransport()
        {
            var client = await GetConnectedClient();

            LastCreatedTransport.Parameters.ClientId.Should().BeNullOrEmpty();
            LastCreatedTransport.Parameters.GetParams().Should().NotContainKey("clientId");
        }

        [Fact]
        [Trait("spec", "RTN2e")]
        public async Task WithBasicAuth_ShouldSetTransportKeyParameter()
        {
            var client = await GetConnectedClient();
            LastCreatedTransport.Parameters.AuthValue.Should().Be(client.Options.Key);
            LastCreatedTransport.Parameters.GetParams().Should().ContainKey("key").WhichValue.Should().Be(client.Options.Key);
        }

        [Fact]
        [Trait("spec", "RTN2e")]
        [Trait("spec", "RSA3c")]
        public async Task WithTokenAuth_ShouldSetTransportAccessTokeParameter()
        {
            var clientId = "123";
            var tokenString = "token";
            var client = await GetConnectedClient(opts =>
            {
                opts.Key = string.Empty;
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
        public async Task ShouldSetTransportVersionParameterToProtocolVersion()
        {
            var client = await GetConnectedClient();

            LastCreatedTransport.Parameters.GetParams()
                .Should().ContainKey("v")
                .WhichValue.Should().Be(Defaults.ProtocolVersion);
        }

        [Fact]
        [Trait("spec", "RTN2g")]
        public async Task ShouldSetTransportLibVersionParameter()
        {
            string pattern = $@"^dotnet(.?\w*)-{Defaults.ProtocolVersion}.(\d+)$";

            // validate the regex pattern
            Regex.Match($"dotnet-{Defaults.ProtocolVersion}.321", pattern).Success.Should().BeTrue();
            Regex.Match($"dotnet.framework-{Defaults.ProtocolVersion}.321", pattern).Success.Should().BeTrue();
            Regex.Match($"dotnet.netstandard20-{Defaults.ProtocolVersion}.0", pattern).Success.Should().BeTrue();
            Regex.Match($"xdotnet-{Defaults.ProtocolVersion}.321", pattern).Success.Should().BeFalse();
            Regex.Match($"csharp.netstandard20-{Defaults.ProtocolVersion}.0", pattern).Success.Should().BeFalse();

            var client = await GetConnectedClient();
            LastCreatedTransport.Parameters.GetParams().Should().ContainKey("lib");
            var transportParams = LastCreatedTransport.Parameters.GetParams();

            // validate the 'lib' param
            Regex.Match(transportParams["lib"], pattern).Success.Should().BeTrue();
        }

        [Fact]
        [Trait("spec", "RTC1f")]
        public async Task WithNullTransportParamsInOptions_ShouldNotThrow()
        {
            var client = await GetConnectedClient(options => options.TransportParams = null);

            var uri = LastCreatedTransport.Parameters.GetUri().ToString();
            Assert.True(true);
        }

        [Fact]
        [Trait("spec", "RTC1f")]
        public async Task WithCustomTransportParamsInOptions_ShouldPassThemInQueryStringWhenCreatingTransport()
        {
            var client = await GetConnectedClient(options => options.TransportParams = new Dictionary<string, object>()
            {
                { "test", "best" },
                { "best", "test" },
            });

            var uri = LastCreatedTransport.Parameters.GetUri().ToString();
            uri.Should().Contain("test=best");
            uri.Should().Contain("best=test");
        }

        [Fact]
        [Trait("spec", "RTC1f1")]
        public async Task WithCustomTransportParamsInOptions_WhichOverrideDefaultValues_ShouldPassTheCustomOneSpecifiedInOptions()
        {
            await GetConnectedClient(options => options.TransportParams = new Dictionary<string, object>()
            {
                { "v", "1000" },
            });

            var uri = LastCreatedTransport.Parameters.GetUri().ToString();
            uri.Should().Contain("v=1000");

            await GetConnectedClient();
            var uri2 = LastCreatedTransport.Parameters.GetUri().ToString();
            uri2.Should().Contain("v=" + Defaults.ProtocolVersion);
        }

        [Theory]
        [MemberData(nameof(TransportParamsValues))]
        [Trait("spec", "RTC1f1")]
        public async Task WithCustomTransportParamsInOptions_AcceptsDifferentTypes_CorrectlyCreatesQueryParams(string name, object value, string expected)
        {
            await GetConnectedClient(options => options.TransportParams = new Dictionary<string, object>()
            {
                { name, value },
            });

            var uri = LastCreatedTransport.Parameters.GetUri().ToString();
            uri.Should().Contain($"{name}={WebUtility.UrlEncode(expected)}");
        }

        public static IEnumerable<object[]> TransportParamsValues
        {
            get
            {
                yield return new object[] { "test", true, "true" };
                yield return new object[] { "test", 4, "4" };
                yield return new object[] { "test", TestParamsType.Instance, "hello-this is a custom& type-" };
            }
        }

        public class TestParamsType
        {
            public static readonly TestParamsType Instance = new TestParamsType();

            public override string ToString()
            {
                return "hello-this is a custom& type-";
            }
        }

        public ConnectionParameterSpecs(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
