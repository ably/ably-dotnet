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
        [Theory]
        [ProtocolData]
        [Trait("spec", "RSC7")]
        public void WithSecureTrue_CreatesSecureRestUrlsWithDefaultHost(Protocol protocol)
        {
            var client = new AblyHttpClient(new AblyHttpOptions { IsSecure = true });

            var url = client.GetRequestUrl(new AblyRequest("/test", HttpMethod.Get, protocol));

            url.Scheme.Should().Be("https");
            url.Host.Should().Be(Defaults.RestHost);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSC7")]
        public void WithSecureFalse_CreatesNonSecureRestUrlsWithDefaultRestHost(Protocol protocol)
        {
            var client = new AblyHttpClient(new AblyHttpOptions { IsSecure = false });

            var url = client.GetRequestUrl(new AblyRequest("/test", HttpMethod.Get, protocol));

            url.Scheme.Should().Be("http");
            url.Host.Should().Be(Defaults.RestHost);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSC7a")]
        [Trait("spec", "G4")]
        public async Task WhenCallingUrl_AddsDefaultAblyLibraryVersionHeader(Protocol protocol)
        {
            var response = new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("Success") };
            var handler = new FakeHttpMessageHandler(response);
            var client = new AblyHttpClient(new AblyHttpOptions(), handler);

            await client.Execute(new AblyRequest("/test", HttpMethod.Get, protocol));
            var values = handler.LastRequest.Headers.GetValues("X-Ably-Version").ToArray();
            values.Should().NotBeEmpty();
            values.First().Should().Be(Defaults.ProtocolVersion);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSC7c")]
        public async Task WhenCallingUrl_AddsRequestIdIfSetTrue(Protocol protocol)
        {
            var response = new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("Success") };
            var handler = new FakeHttpMessageHandler(response);
            var client = new AblyHttpClient(new AblyHttpOptions { AddRequestIds = true }, handler);
            var ablyRequest = new AblyRequest("/test", HttpMethod.Get, protocol);
            ablyRequest.AddHeaders(new Dictionary<string, string> { { "request_id", "custom_request_id" } });
            await client.Execute(ablyRequest);
            var values = handler.LastRequest.Headers.GetValues("request_id").ToArray();
            values.Should().NotBeEmpty();
            values.First().Should().StartWith("custom_request_id");
        }

        [Theory]
        [ProtocolData]
        public async Task WhenCallingUrlWithPostParamsAndEmptyBody_PassedTheParamsAsUrlEncodedValues(Protocol protocol)
        {
            var response = new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("Success") };
            var handler = new FakeHttpMessageHandler(response);
            var client = new AblyHttpClient(new AblyHttpOptions(), handler);

            var ablyRequest = new AblyRequest("/test", HttpMethod.Post, protocol)
            {
                PostParameters = new Dictionary<string, string> { { "test", "test" }, { "best", "best" } },
            };

            await client.Execute(ablyRequest);
            var content = handler.LastRequest.Content;
            var formContent = content as FormUrlEncodedContent;
            formContent.Should().NotBeNull("Content should be of type FormUrlEncodedContent");
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSC7d")]
        public async Task WhenCallingUrl_AddsDefaultAblyAgentHeader(Protocol protocol)
        {
            var response = new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("Success") };
            var handler = new FakeHttpMessageHandler(response);
            var client = new AblyHttpClient(new AblyHttpOptions(), handler);

            await client.Execute(new AblyRequest("/test", HttpMethod.Get, protocol));
            string[] values = handler.LastRequest.Headers.GetValues("Ably-Agent").ToArray();
            values.Should().HaveCount(1);
            string[] agentValues = values[0].Split(' ');

            var keys = new List<string>()
            {
                "ably-dotnet/",
                Agent.DotnetRuntimeIdentifier(),
                Agent.OsIdentifier()
            };

            Agent.DotnetRuntimeIdentifier().Split('/').Length.Should().Be(2);

            keys.RemoveAll(s => s.IsEmpty());

            agentValues.Should().HaveCount(keys.Count);
            for (var i = 0; i < keys.Count; ++i)
            {
                agentValues[i].StartsWith(keys[i]).Should().BeTrue($"'{agentValues[i]}' should start with '{keys[i]}'");
            }
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSC7d6")]
        public async Task WhenCallingUrl_AddsCustomizedAblyAgentHeader(Protocol protocol)
        {
            var response = new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("Success") };
            var handler = new FakeHttpMessageHandler(response);

            var ablyHttpOptions = new AblyHttpOptions
            {
                Agents = new Dictionary<string, string>
                {
                    { "agent1", "value1" },
                    { "agent2", "value2" },
                },
            };

            var client = new AblyHttpClient(ablyHttpOptions, handler);

            await client.Execute(new AblyRequest("/test", HttpMethod.Get, protocol));
            string[] values = handler.LastRequest.Headers.GetValues("Ably-Agent").ToArray();
            values.Should().HaveCount(1);
            string[] agentValues = values[0].Split(' ');

            var keys = new List<string>()
            {
                "ably-dotnet/",
                Agent.DotnetRuntimeIdentifier(),
                Agent.OsIdentifier(),
                "agent1",
                "agent2",
            };

            keys.RemoveAll(s => s.IsEmpty());

            agentValues.Should().HaveCount(keys.Count);
            for (var i = 0; i < keys.Count; ++i)
            {
                agentValues[i].StartsWith(keys[i]).Should().BeTrue($"'{agentValues[i]}' should start with '{keys[i]}'");
            }
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
