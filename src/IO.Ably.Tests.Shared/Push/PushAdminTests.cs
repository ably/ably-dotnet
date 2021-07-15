using System;
using System.Net;
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

        [Fact]
        [Trait("spec", "RSH1a")]
        public async Task Publish_ShouldMakeRequest_ToCorrectUrl()
        {
            bool requestCompleted = false;
            var recipient = JObject.FromObject(new { transportType = "fcm", registrationToken = "token" });
            var payload = JObject.FromObject(new { data = "data" });

            var rest = GetRestClient(request =>
            {
                request.Url.Should().Be("/push/publish");
                var data = (JObject) request.PostData;
                data.Should().NotBeNull();
                // Recipient should be set in the recipient property
                ((JObject)data["recipient"]).Should().BeSameAs(recipient);

                var expected = payload.DeepClone();
                expected["recipient"] = recipient;
                data.Should().BeEquivalentTo(expected);

                requestCompleted = true;

                return Task.FromResult(new AblyResponse() { StatusCode = HttpStatusCode.Accepted });
            });

            Func<Task> publishAction = async () => await rest.Push.Admin.PublishAsync(recipient, payload);

            await publishAction.Should().NotThrowAsync();

            requestCompleted.Should().BeTrue();
        }

        public PushAdminTests(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
