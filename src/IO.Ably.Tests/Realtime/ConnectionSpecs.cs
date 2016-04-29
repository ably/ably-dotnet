using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Transport;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    public class FakeTransportFactory : ITransportFactory
    {
        public FakeTransport LastCreatedTransport { get; set; }

        public FakeTransportFactory()
        {
        }

        public Task<ITransport> CreateTransport(TransportParams parameters)
        {
            LastCreatedTransport = new FakeTransport(parameters);
            return Task.FromResult<ITransport>(LastCreatedTransport);
        }
    }

    public class ConnectionSpecs : AblyRealtimeSpecs
    {
        public ConnectionSpecs(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        [Trait("spec", "RTN1")]
        public void ShouldUseWebSocketTransport()
        {
            var client = GetRealtimeClient();

            client.ConnectionManager.Transport.GetType().Should().Be(typeof(WebSocketTransport));
        }

        [Trait("spec", "RTN2")]
        public class ConnectionParameterTests : ConnectionSpecs
        {
            private FakeTransportFactory _fakeTransportFactory;
            private FakeTransport LastCreatedTransport => _fakeTransportFactory.LastCreatedTransport;

            private AblyRealtime GetClientWithFakeTransport(Action<ClientOptions> optionsAction = null)
            {
                var options = new ClientOptions(ValidKey) {TransportFactory = _fakeTransportFactory};
                optionsAction?.Invoke(options);
                var client = GetRealtimeClient(options);
                return client;
            }

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
            public void WithBasicAuth_ShouldSetTransportKeyParameter()
            {
                var client = GetClientWithFakeTransport();
                LastCreatedTransport.Parameters.AuthValue.Should().Be(client.Options.Key);
                LastCreatedTransport.Parameters.GetParams().
                    Should().ContainKey("key")
                    .WhichValue.Should().Be(client.Options.Key);
            }


            


            public ConnectionParameterTests(ITestOutputHelper output) : base(output)
            {
                _fakeTransportFactory = new FakeTransportFactory();
            }
        }
    }
}
