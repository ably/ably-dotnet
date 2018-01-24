using Xunit;

namespace IO.Ably.Tests
{
    public class DataRequestQueryParseTests
    {
        private HistoryRequestParams _query;
        public const string HeaderValue = "?start=1380794880000&end=1380794881058&limit=100&by=minute&direction=forwards&format=json&first_start=1380794880000";

        public DataRequestQueryParseTests()
        {
            _query = HistoryRequestParams.Parse(HeaderValue);
        }

        [Fact]
        public void Parse_WithQueryString_SetsCorrectStartAndEndDates()
        {
            //Arrange
            var startDate = long.Parse("1380794880000").FromUnixTimeInMilliseconds();
            var endDate = long.Parse("1380794881058").FromUnixTimeInMilliseconds();
            
            //Assert
            Assert.Equal(startDate, _query.Start);
            Assert.Equal(endDate, _query.End);
        }

        [Fact]
        public void Parse_WithQueryString_SetsCorrectLimit()
        {
            Assert.Equal(100, _query.Limit);
        }

        [Fact]
        public void Parse_SetsCorrectDirection()
        {
            //Assert
            Assert.Equal(QueryDirection.Forwards, _query.Direction);
        }

        [Fact]
        public void Parse_HasTwoExtraParameters()
        {
            //Assert
            Assert.Equal(3, _query.ExtraParameters.Count);
            Assert.True(_query.ExtraParameters.ContainsKey("by"));
            Assert.True(_query.ExtraParameters.ContainsKey("first_start"));
            Assert.True(_query.ExtraParameters.ContainsKey("format"));
        }
        
    }
}