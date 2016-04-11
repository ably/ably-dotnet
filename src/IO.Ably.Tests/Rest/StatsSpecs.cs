using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using static Xunit.Assert;

namespace IO.Ably.Tests
{
    public class StatsSpecs : AblySpecs
    {
        private AblyRequest _lastRequest;
        private const string ValidKey = "1iZPfA.BjcI_g:wpNhw5RCw6rDjisl";

        private AblyRest GetRestClient(Func<AblyRequest, Task<AblyResponse>> handleRequestFunc = null)
        {
            var client = new AblyRest(new ClientOptions(ValidKey) { UseBinaryProtocol = false});
            client.ExecuteHttpRequest = request =>
            {
                _lastRequest = request;
                if (handleRequestFunc != null)
                {
                    return handleRequestFunc(request);
                }
                return "[{}]".ToAblyResponse();
            };
            return client;
        }

        [Fact]
        public async Task ShouldCreateRequestToCorrectPath()
        {
            var rest = GetRestClient();

            await rest.Stats();

            Equal(HttpMethod.Get, _lastRequest.Method);
            Equal("/stats", _lastRequest.Url);
        }


        [Fact]
        public async Task ShouldSetCorrectHeaders()
        {
            var rest = GetRestClient();

            var query = new StatsDataRequestQuery
            {
                Start = Now.AddHours(-1),
                End = Now,
                Direction = QueryDirection.Forwards,
                Limit = 1000
            };

            await rest.Stats(query);

            _lastRequest.AssertContainsParameter("start", query.Start.Value.ToUnixTimeInMilliseconds().ToString());
            _lastRequest.AssertContainsParameter("end", query.End.Value.ToUnixTimeInMilliseconds().ToString());
            _lastRequest.AssertContainsParameter("direction", query.Direction.ToString().ToLower());
            _lastRequest.AssertContainsParameter("limit", query.Limit.Value.ToString());
        }

        [Fact]
        public async Task ShouldReturnCorrectFirstAndNextLinks()
        {
            //Arrange
            var rest = GetRestClient(request =>
            {
                var response = new AblyResponse()
                {
                    Headers = DataRequestQueryTests.GetSampleStatsRequestHeaders(),
                    TextResponse = "[{}]"
                };
                return response.ToTask();
            });

            //Act
            var result = await rest.Stats();

            //Assert
            NotNull(result.NextQuery);
            NotNull(result.FirstQuery);
        }

        private async Task ExecuteStatsQuery(StatsDataRequestQuery query)
        {
            var rest = GetRestClient();

            await rest.Stats(query);
        }

        [Theory]
        [Trait("spec", "RSC6b1")]
        [InlineData(0, 0, QueryDirection.Backwards)]
        [InlineData(0, 0, QueryDirection.Forwards)]
        [InlineData(0, 1, QueryDirection.Forwards)]
        [InlineData(0, 1, QueryDirection.Backwards)]
        public async Task ShouldAcceptStartAndEndDateTimes(int startOffset, int endOffset, QueryDirection direction)
        {
            var query = new StatsDataRequestQuery
            {
                Start = Now.AddHours(startOffset),
                End = Now.AddHours(endOffset),
                Direction = direction
            };

            await ExecuteStatsQuery(query);

            _lastRequest.AssertContainsParameter("start", query.Start.Value.ToUnixTimeInMilliseconds().ToString());
            _lastRequest.AssertContainsParameter("end", query.End.Value.ToUnixTimeInMilliseconds().ToString());
        }

        [Theory]
        [Trait("spec", "RSC6b1")]
        [InlineData(1, 0, QueryDirection.Forwards)]
        [InlineData(1, 0, QueryDirection.Backwards)]
        public void ShouldThrowIfStartIsGreaterThanEnd(int startOffset, int endOffset, QueryDirection direction)
        {
            var query = new StatsDataRequestQuery
            {
                Start = Now.AddHours(startOffset),
                End = Now.AddHours(endOffset),
                Direction = direction
            };

            ThrowsAsync<AblyException>(() => ExecuteStatsQuery(query));
        }

        [Theory]
        [InlineData(QueryDirection.Forwards)]
        [InlineData(QueryDirection.Backwards)]
        [InlineData(null)]
        [Trait("spec", "RSC6b2")]
        public async Task ShouldPassDirectionToRequestWithBackwardsAsDefault(QueryDirection? direction)
        {
            var query = new StatsDataRequestQuery
            {
                Start = Now,
                End = Now
            };
            if (direction.HasValue)
                query.Direction = direction.Value;

            await ExecuteStatsQuery(query);

            var expectedDirection = direction.GetValueOrDefault(QueryDirection.Backwards).ToString().ToLower();
            _lastRequest.AssertContainsParameter("direction", expectedDirection);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(1000)]
        [InlineData(null)]
        [Trait("spec", "RSC6b3")]
        public async Task ShouldPassLimitWithDefaultof100(int? limit)
        {
            var query = new StatsDataRequestQuery();
            if (limit.HasValue)
                query.Limit = limit.Value;

            await ExecuteStatsQuery(query);

            _lastRequest.AssertContainsParameter("limit", limit.GetValueOrDefault(100).ToString());
        }

        [Theory]
        [InlineData(-10)]
        [InlineData(1001)]
        [Trait("spec", "RSCb3")]
        public void ShouldThrowIfLimitExceeds1000orLessThan0(int limit)
        {
            ThrowsAsync<AblyException>(() => ExecuteStatsQuery(new StatsDataRequestQuery() {Limit = limit}));
        }

        [Theory]
        [InlineData(StatsBy.Month)]
        [InlineData(StatsBy.Day)]
        [InlineData(StatsBy.Hour)]
        [InlineData(StatsBy.Minute)]
        [InlineData(null)]
        [Trait("spec", "RSC6b4")]
        public async Task ShouldPassStatsByToQueryWithDefaultOfMinute(StatsBy? statsBy)
        {
            var query = new StatsDataRequestQuery();
            if (statsBy.HasValue)
                query.By = statsBy.Value;

            await ExecuteStatsQuery(query);

            _lastRequest.AssertContainsParameter("by", statsBy.GetValueOrDefault(StatsBy.Minute).ToString().ToLower());
        }

    }


    [Collection("Stats SandBox Collection")]
    public class StatsSandBoxSpecs
    {
        private readonly AblySandboxFixture _fixture;

        public StatsSandBoxSpecs(AblySandboxFixture fixture)
        {
            _fixture = fixture;
        }

        private const string ValidKey = "1iZPfA.BjcI_g:wpNhw5RCw6rDjisl";

        private async Task<AblyRest> GetRestClient(Protocol protocol)
        {
            var settings = await _fixture.GetSettings();
            var defaultOptions = settings.CreateDefaultOptions();
            defaultOptions.UseBinaryProtocol = protocol == Protocol.MsgPack;
            return new AblyRest(defaultOptions);
        }
    }
}