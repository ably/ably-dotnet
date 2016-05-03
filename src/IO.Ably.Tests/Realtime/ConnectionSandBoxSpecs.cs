using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Collection("AblyRest SandBox Collection")]
    [Trait("requires", "sandbox")]
    public class ConnectionSandBoxSpecs : SandboxSpecs
    {
        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN6")]
        public async Task WithAutoConnectTrue_ShouldConnectToAblyInTheBackground(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);
            var awaitor = new ConnectionAwaiter(client.Connection);
            await awaitor.Wait();
            client.Connection.State.Should().Be(ConnectionStateType.Connected);
        }

        public ConnectionSandBoxSpecs(AblySandboxFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }
    }
}