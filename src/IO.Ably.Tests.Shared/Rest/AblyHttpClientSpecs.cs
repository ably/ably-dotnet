using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
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
            var client = new AblyHttpClient(new AblyHttpOptions() { IsSecure = true });

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
            var response = new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("Success") };
            var handler = new FakeHttpMessageHandler(response);
            var client = new AblyHttpClient(new AblyHttpOptions(), handler);

            await client.Execute(new AblyRequest("/test", HttpMethod.Get));
            var values = handler.LastRequest.Headers.GetValues("X-Ably-Lib");
            var fileVersion = typeof(Defaults).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
            values.Should().NotBeEmpty();
            values.First().Should().Be("dotnet-" + fileVersion);
        }

        [Fact]
        public async Task WhenCallingUrl_AddsDefaultAblyLibraryVersionHeader()
        {
            var response = new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("Success") };
            var handler = new FakeHttpMessageHandler(response);
            var client = new AblyHttpClient(new AblyHttpOptions(), handler);

            await client.Execute(new AblyRequest("/test", HttpMethod.Get));
            var values = handler.LastRequest.Headers.GetValues("X-Ably-Version");
            values.Should().NotBeEmpty();
            values.First().Should().Be(Defaults.ProtocolVersion);
        }

        [Fact]
        public async Task WhenCallingUrlWithPostParamsAndEmptyBody_PassedTheParamsAsUrlEncodedValues()
        {
            var response = new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("Success") };
            var handler = new FakeHttpMessageHandler(response);
            var client = new AblyHttpClient(new AblyHttpOptions(), handler);


            var ablyRequest = new AblyRequest("/test", HttpMethod.Post);
            ablyRequest.PostParameters = new Dictionary<string, string>() { { "test", "test" }, { "best", "best" } };

            await client.Execute(ablyRequest);
            var content = handler.LastRequest.Content;
            var formContent = content as FormUrlEncodedContent;
            formContent.Should().NotBeNull("Content should be of type FormUrlEncodedContent");
        }

        public class IsRetriableResponseSpecs
        {
            private AblyHttpClient _client;

            public IsRetriableResponseSpecs()
            {
                _client = new AblyHttpClient(new AblyHttpOptions());
            }

            [Fact]
            public void IsRetryableError_WithTaskCancellationException_ShouldBeTrue()
            {
                _client.IsRetryableError(new TaskCanceledException()).Should().BeTrue();
            }

            [Theory]
            [InlineData(WebExceptionStatus.Timeout)]
            [InlineData(WebExceptionStatus.ConnectFailure)]
            [InlineData(WebExceptionStatus.NameResolutionFailure)]
            [Trait("spec", "RSC15d")]
            public void IsRetyableError_WithHttpMessageExecption_ShouldBeTrue(WebExceptionStatus status)
            {
                var exception = new HttpRequestException("Error", new WebException("boo", status));
                _client.IsRetryableError(exception).Should().BeTrue();
            }

            [Theory]
            [InlineData(HttpStatusCode.BadGateway, true)]
            [InlineData(HttpStatusCode.InternalServerError, true)]
            [InlineData(HttpStatusCode.NotImplemented, true)]
            [InlineData(HttpStatusCode.BadGateway, true)]
            [InlineData(HttpStatusCode.ServiceUnavailable, true)]
            [InlineData(HttpStatusCode.GatewayTimeout, true)]
            [InlineData(HttpStatusCode.NoContent, false)]
            [InlineData(HttpStatusCode.NotFound, false)]
            [Trait("spec", "RSC15d")]

            public void IsRetryableResponse_WithErrorCode_ShouldReturnExpectedValue(HttpStatusCode statusCode,
                bool expected)
            {
                var response = new HttpResponseMessage(statusCode);
                _client.IsRetryableResponse(response).Should().Be(expected);
            }
        }
    }
}