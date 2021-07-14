using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.DotNetCore20.Push
{
    [Trait("spec", "RSH1")]
    public class PushAdminTests : MockHttpRestSpecs
    {
        [Fact]
        [Trait("spec", "RSH1a")]
        public async Task Publish_WithEmptyRecipientOrData_ShouldThrow()
        {
            var rest = GetRestClient();

            var recipientEx = await Assert.ThrowsAsync<AblyException>(() => rest.Push.Admin.PublishAsync(null, new JObject()));
            recipientEx.ErrorInfo.Code.Should().Be(ErrorCodes.BadRequest);

            var dataEx = await Assert.ThrowsAsync<AblyException>(() => rest.Push.Admin.PublishAsync(new JObject(), null));
            dataEx.ErrorInfo.Code.Should().Be(ErrorCodes.BadRequest);
        }

        public PushAdminTests(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
