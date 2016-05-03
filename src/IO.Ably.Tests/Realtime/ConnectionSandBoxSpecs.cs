using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Collection("AblyRest SandBox Collection")]
    [Trait("requires", "sandbox")]
    public class ConnectionSandBoxSpecs : SandboxSpecs
    {
        [Fact]
        public void WithAutoConnectTrue_ShouldConnectToAblyInTheBackground()
        {
            
        }

        public ConnectionSandBoxSpecs(AblySandboxFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }
    }
}