using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Transport;
using Xunit;

namespace IO.Ably.Tests
{
    public class AblyHttpClientSpecs
    {
        [Fact]
        [Trait("spec", "RSC7")]
        public void WithSecureTrue_CreatesSecureRestUrlsWithDefaultHost()
        {
            var client = new AblyHttpClient(new AblyHttpOptions() { IsSecure = true});

            var url = client.GetRequestUrl(new AblyRequest("/test", HttpMethod.Get));

            url.Scheme.Should().Be("https");
            url.Host.Should().Be(Defaults.RestHost);
        }

        [Fact]
        [Trait("spec", "RSC7")]
        public void WithSecureFalse_CreatesNonSecureRestUrlsWithDefaultRestHost()
        {
            var client = new AblyHttpClient(new AblyHttpOptions() { IsSecure = false });

            var url = client.GetRequestUrl(new AblyRequest("/test", HttpMethod.Get));

            url.Scheme.Should().Be("http");
            url.Host.Should().Be(Defaults.RestHost);
        }

        [Fact]
        [Trait("spec", "RSC7a")]
        public async Task WhenCallingUrl_AddsDefaultAblyHeader()
        {
            var response = new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("Success")};
            var handler = new FakeHttpMessageHandler(response);
            var client = new AblyHttpClient(new AblyHttpOptions(), handler);

            await client.Execute(new AblyRequest("/test", HttpMethod.Get));
            var values = handler.LastRequest.Headers.GetValues("X-Ably-Version");
            values.Should().NotBeEmpty();
            values.First().Should().Be("0.8");
        }


    }
}