using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Rest;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public class PresenceSpecs : MockHttpSpecs
    {
        [Fact]
        [Trait("spec", "RSP1")]
        [Trait("spec", "RSP3a")]
        public async Task Presence_CreatesGetRequestWithCorrectPath()
        {
            var rest = GetRestClient();

            var channel = rest.Channels.Get("Test");

            var presence = await channel.Presence.Get();

            presence.Should().BeOfType<PaginatedResult<PresenceMessage>>();

            Assert.Equal(HttpMethod.Get, LastRequest.Method);
            Assert.Equal($"/channels/{channel.Name}/presence", LastRequest.Url);
        }

        public class GetSpecs : PresenceSpecs
        {
            private AblyRest _client;
            private IChannel _channel;


            [Theory]
            [InlineData(null, "100", false)]
            [InlineData(500, "500", false)]
            [InlineData(-1, "", true)]
            [InlineData(1001, "", true)]
            [Trait("spec", "RSP3a2")]
            public async Task WithLimitParameter_ShouldSetLimitHeaderOrThrowForInvalidValues(int? limit, string expectedLimitHeader, bool throws)
            {
                if (throws)
                {
                    var ex = await Assert.ThrowsAsync<ArgumentException>(() => _channel.Presence.Get(limit));
                }
                else
                {
                    var result = await _channel.Presence.Get(limit);

                    LastRequest.AssertContainsParameter("limit", expectedLimitHeader);
                }
            }

            public GetSpecs(ITestOutputHelper output) : base(output)
            {
                _client = GetRestClient();
                _channel = _client.Channels.Get("test");
            }
        }

        public PresenceSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}