using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    [Trait("requires", "sandbox")]
    public class RequestSandBoxSpecs : SandboxSpecs
    {
        public readonly static DateTimeOffset StartInterval = DateHelper.CreateDate(DateTimeOffset.UtcNow.Year - 1, 2, 3, 15, 5);

        public async Task<List<Stats>> GetStats(Protocol protocol)
        {
            var client = await GetRestClient(protocol);
            var result = await client.StatsAsync(new StatsRequestParams() { Start = StartInterval.AddMinutes(-2), End = StartInterval.AddMinutes(1) });

            return result.Items;
        }

        [Trait("spec", "RSC19")]
        [Theory]
        [ProtocolData]
        public async Task SimpleRequest(Protocol protocol)
        {
            var channelName = "simple-request-test".AddRandomSuffix();
            var client = await GetRestClient(protocol);
            AblyRequest req = null;
            var exec = client.ExecuteHttpRequest;
            client.ExecuteHttpRequest = request =>
            {
                req = request;
                return exec(request);
            };

            var testParams = new Dictionary<string, string>();
            testParams.Add("testParams", "testValue");
            var testHeaders = new Dictionary<string, string>();
            testHeaders.Add("x-test-header", "testValue");

            var result = await client.Request(HttpMethod.Get, "/channels/" + channelName, testParams, null, testHeaders);
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.OK); // 200
            result.Success.Should().BeTrue();
            result.ErrorCode.Should().Be(0);
            result.ErrorMessage.Should().BeNull();
            result.Items.Should().HaveCount(1);
            result.Items.First().Should().BeOfType<JObject>();
            var channelDetails = result.Items.First() as JObject; // cast from JToken
            channelDetails["id"].ToString().Should().BeEquivalentTo(channelName);
            req.Headers.ContainsKey("Authorization").Should().BeTrue();
            req.Headers["Accept"].Should().Contain("application/json");
        }

        public RequestSandBoxSpecs(ITestOutputHelper output)
            : base(new AblySandboxFixture(), output)
        {
        }
    }
}
