using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Tests.Infrastructure;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.DotNetCore20.Push
{
    public static class PushAdminSandboxTests
    {
        public class PublishTests : SandboxSpecs
        {
            [Theory]
            [ProtocolData]
            [Trait("spec", "RSH1a")]
            public async Task ShouldSuccessfullyPublishAPayload(Protocol protocol)
            {
                var client = await GetRealtimeClient(protocol);
                var channelName = "pushenabled:test".AddRandomSuffix();


                var channel = client.Channels.Get(channelName);
                await channel.AttachAsync();
                var awaiter = new TaskCompletionAwaiter();
                var pushPayload = JObject.FromObject(new { notification = new { title = "test", body = "message body" }, data = new { foo = "bar" }, });
                channel.Subscribe(message =>
                {
                    message.Name.Should().Be("__ably_push__");
                    var payload = JObject.Parse((string)message.Data);
                    payload["data"].Should().BeEquivalentTo(pushPayload["data"]);
                    ((string)payload["notification"]["title"]).Should().Be("test");
                    ((string)payload["notification"]["body"]).Should().Be("message body");

                    awaiter.SetCompleted();
                });

                var host = "https://" + client.Options.FullRestHost();
                var key = client.Options.Key;

                var pushRecipient = JObject.FromObject(new
                {
                    transportType = "ablyChannel",
                    channel = channelName,
                    ablyKey = key,
                    ablyUrl = host,
                });
                await client.Push.Admin.PublishAsync(pushRecipient, pushPayload);

                var result = await awaiter.Task;
                result.Should().BeTrue();
            }

            public PublishTests(AblySandboxFixture fixture, ITestOutputHelper output)
                : base(fixture, output)
            {
            }
        }
    }
}
