using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace IO.Ably.Tests
{
    public class AblyHttpClientSpecs
    {
        [Fact]
        [Trait("spec", "RSC7")]
        public void WithSecureTrue_CreatesSecureRestUrlsWithDefaultHost()
        {
            var client = new AblyHttpClient(new AblyHttpOptions { IsSecure = true });

            var url = client.GetRequestUrl(new AblyRequest("/test", HttpMethod.Get));

            url.Scheme.Should().Be("https");
            url.Host.Should().Be(Defaults.RestHost);
        }

        [Fact]
        [Trait("spec", "RSC7")]
        public void WithSecureFalse_CreatesNonSecureRestUrlsWithDefaultRestHost()
        {
            var client = new AblyHttpClient(new AblyHttpOptions { IsSecure = false });

            var url = client.GetRequestUrl(new AblyRequest("/test", HttpMethod.Get));

            url.Scheme.Should().Be("http");
            url.Host.Should().Be(Defaults.RestHost);
        }

        [Fact]
        [Trait("spec", "RSC7a")]
        [Trait("spec", "G4")]
        public async Task WhenCallingUrl_AddsDefaultAblyLibraryVersionHeader()
        {
            var response = new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("Success") };
            var handler = new FakeHttpMessageHandler(response);
            var client = new AblyHttpClient(new AblyHttpOptions(), handler);

            await client.Execute(new AblyRequest("/test", HttpMethod.Get));
            var values = handler.LastRequest.Headers.GetValues("X-Ably-Version").ToArray();
            values.Should().NotBeEmpty();
            values.First().Should().Be(Defaults.ProtocolVersion);
        }

        [Fact]
        [Trait("spec", "RSC7b")]
        public async Task WhenCallingUrl_AddsDefaultAblyHeader()
        {
            var response = new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("Success") };
            var handler = new FakeHttpMessageHandler(response);
            var client = new AblyHttpClient(new AblyHttpOptions(), handler);

            await client.Execute(new AblyRequest("/test", HttpMethod.Get));
            var values = handler.LastRequest.Headers.GetValues("X-Ably-Lib").ToArray();
            values.Should().NotBeEmpty();
            values.First().Should().StartWith("dotnet");
            values.First().Should().Be(Defaults.LibraryVersion);
            Defaults.LibraryVersion.Should().BeEquivalentTo($"dotnet.{IoC.PlatformId}-{Defaults.AssemblyVersion}");
        }

        [Fact]
        [Trait("spec", "RSC7c")]
        public async Task WhenCallingUrl_AddsRequestIdIfSetTrue()
        {
            var response = new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("Success") };
            var handler = new FakeHttpMessageHandler(response);
            var client = new AblyHttpClient(new AblyHttpOptions { AddRequestIds = true }, handler);
            var ablyRequest = new AblyRequest("/test", HttpMethod.Get);
            ablyRequest.AddHeaders(new Dictionary<string, string> { { "request_id", "custom_request_id" } });
            await client.Execute(ablyRequest);
            var values = handler.LastRequest.Headers.GetValues("request_id").ToArray();
            values.Should().NotBeEmpty();
            values.First().Should().StartWith("custom_request_id");
        }

        [Fact]
        public async Task WhenCallingUrlWithPostParamsAndEmptyBody_PassedTheParamsAsUrlEncodedValues()
        {
            var response = new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("Success") };
            var handler = new FakeHttpMessageHandler(response);
            var client = new AblyHttpClient(new AblyHttpOptions(), handler);

            var ablyRequest = new AblyRequest("/test", HttpMethod.Post)
            {
                PostParameters = new Dictionary<string, string> { { "test", "test" }, { "best", "best" } },
            };

            await client.Execute(ablyRequest);
            var content = handler.LastRequest.Content;
            var formContent = content as FormUrlEncodedContent;
            formContent.Should().NotBeNull("Content should be of type FormUrlEncodedContent");
        }

        public class IsRetryableResponseSpecs
        {
            [Fact]
            public void IsRetryableError_WithTaskCancellationException_ShouldBeTrue()
            {
                AblyHttpClient.IsRetryableError(new TaskCanceledException()).Should().BeTrue();
            }

            [Theory]
            [InlineData(WebExceptionStatus.Timeout)]
            [InlineData(WebExceptionStatus.ConnectFailure)]
            [InlineData(WebExceptionStatus.NameResolutionFailure)]
            [Trait("spec", "RSC15d")]
            public void IsRetryableError_WithHttpMessageException_ShouldBeTrue(WebExceptionStatus status)
            {
                var exception = new HttpRequestException("Error", new WebException("boo", status));
                AblyHttpClient.IsRetryableError(exception).Should().BeTrue();
            }

            [Theory]
            [InlineData(HttpStatusCode.BadGateway, true)]
            [InlineData(HttpStatusCode.InternalServerError, true)]
            [InlineData(HttpStatusCode.NotImplemented, true)]
            [InlineData(HttpStatusCode.ServiceUnavailable, true)]
            [InlineData(HttpStatusCode.GatewayTimeout, true)]
            [InlineData(HttpStatusCode.NoContent, false)]
            [InlineData(HttpStatusCode.NotFound, false)]
            [Trait("spec", "RSC15d")]
            public void IsRetryableResponse_WithErrorCode_ShouldReturnExpectedValue(
                HttpStatusCode statusCode,
                bool expected)
            {
                var response = new HttpResponseMessage(statusCode);
                AblyHttpClient.IsRetryableResponse(response).Should().Be(expected);
            }
        }
    }
}
