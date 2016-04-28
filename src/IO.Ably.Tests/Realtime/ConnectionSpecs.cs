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

    public class ConnectionSpecs : AblySpecs
    {


        public ConnectionSpecs(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        [Trait("spec", "RTN1")]
        public async Task ShouldUseWebSocketTransport()
        {
            var client = GetClient();

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
                return GetClient(options);
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
            public void WithUseBinaryEncoding_ShouldSetTransportFormatProperty(bool useBinary, string format)
            {
                var client = GetClientWithFakeTransport(opts => opts.UseBinaryProtocol = useBinary);
                LastCreatedTransport.Parameters.UseBinaryProtocol.Should().Be(useBinary);
                LastCreatedTransport.Parameters.GetParams().Should().ContainKey("format").WhichValue(format);
            }

            public ConnectionParameterTests(ITestOutputHelper output) : base(output)
            {
                _fakeTransportFactory = new FakeTransportFactory();
            }
        }

        protected virtual AblyRealtime GetClient(ClientOptions options = null)
        {
            var clientOptions = options ?? new ClientOptions(ValidKey);
            return new AblyRealtime(clientOptions);
        }
        protected virtual AblyRealtime GetClient(Action<ClientOptions> optionsAction)
        {
            var options = new ClientOptions(ValidKey);
            optionsAction?.Invoke(options);
            return new AblyRealtime(options);
        }
    }

    
}
