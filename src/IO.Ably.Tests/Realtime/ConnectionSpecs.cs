using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    public class ConnectionSpecs : AblyRealtimeSpecs
    {
        private FakeTransportFactory _fakeTransportFactory;
        protected FakeTransport LastCreatedTransport => _fakeTransportFactory.LastCreatedTransport;

        protected AblyRealtime GetClientWithFakeTransport(Action<ClientOptions> optionsAction = null)
        {
            var options = new ClientOptions(ValidKey) { TransportFactory = _fakeTransportFactory };
            optionsAction?.Invoke(options);
            var client = GetRealtimeClient(options);
            return client;
        }

        public ConnectionSpecs(ITestOutputHelper output) : base(output)
        {
            _fakeTransportFactory = new FakeTransportFactory();
        }

        public class GeneralTests : ConnectionSpecs
        {
            [Fact]
            [Trait("spec", "RTN1")]
            public void ShouldUseWebSocketTransport()
            {
                var client = GetRealtimeClient();

                client.ConnectionManager.Transport.GetType().Should().Be(typeof(WebSocketTransport));
            }

            [Fact]
            [Trait("spec", "RTN3")]
            [Trait("sandboxTest", "needed")]
            public void WithAutoConnect_CallsConnectOnTransport()
            {
                var client = GetClientWithFakeTransport(opts => opts.AutoConnect = true);

                client.ConnectionManager.ConnectionState.Should().Be(ConnectionStateType.Connected);
                LastCreatedTransport.ConnectCalled.Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RTN3")]
            public void WithAutoConnectFalse_LeavesStateAsInitialized()
            {
                var client = GetClientWithFakeTransport(opts => opts.AutoConnect = false);

                client.ConnectionManager.ConnectionState.Should().Be(ConnectionStateType.Initialized);
                LastCreatedTransport.Should().BeNull("Transport shouldn't be created without calling connect when AutoConnect is false");
            }

            public GeneralTests(ITestOutputHelper output) : base(output)
            {
            }
        }

        [Trait("spec", "RTN2")]
        public class ConnectionParameterSpecs : ConnectionSpecs
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
                var clientId = "123";
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

            public ConnectionParameterSpecs(ITestOutputHelper output) : base(output)
            {
                
            }
        }

    }
}
