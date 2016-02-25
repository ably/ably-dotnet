using System.Collections.Specialized;
using System.Net;
using Xunit;

namespace IO.Ably.Tests
{
    public class DataRequestQueryTests
    {
        public static WebHeaderCollection GetSampleHistoryRequestHeaders()
        {
            var headers = new WebHeaderCollection();

            headers.Add("Link", string.Format("<./history{0}>; rel=\"first\"", FirstQueryString));
            headers.Add("Link", string.Format("<./history{0}>; rel=\"next\"", NextQueryString));
            headers.Add("Link", string.Format("<./history{0}>; rel=\"current\"", CurrentQueryString));
            return headers;
        }

        public static WebHeaderCollection GetSampleStatsRequestHeaders()
        {
            var headers = new WebHeaderCollection();

            headers.Add("Link", string.Format("<./stats{0}>; rel=\"first\"", FirstQueryString));
            headers.Add("Link", string.Format("<./stats{0}>; rel=\"next\"", NextQueryString));
            return headers;
        }

        public const string FirstQueryString = "?start=1380794880000&end=1380794881058&limit=100&by=minute&direction=forwards&format=json&first_start=1380794880000";
        public const string CurrentQueryString = "?start=1380794880000&end=1380794881058&limit=100&by=minute&direction=forwards&format=json&first_start=1380794880000";
        public const string NextQueryString = "?start=1380794881111&end=1380794881058&limit=100&by=minute&direction=forwards&format=json&first_start=1380794880000";

        [Fact]
        public void GetLinkQuery_WithHeadersAndAskingForNextLink_ReturnsCorrectRequestQuery()
        {
            //Arrange

            var nextDataRequest = DataRequestQuery.Parse(NextQueryString);

            //Act
            var actual = DataRequestQuery.GetLinkQuery(GetSampleHistoryRequestHeaders(), "next");

            //Assert
            Assert.Equal(nextDataRequest, actual);
        }

        [Fact]
        public void GetLinkQuery_WithHeadersAndAskingForFirstLink_ReturnsCorrectRequestQuery()
        {
            //Arrange
            var firstDataRequest =
                DataRequestQuery.Parse(
                    FirstQueryString);

            //Act
            //Act
            var actual = DataRequestQuery.GetLinkQuery(GetSampleHistoryRequestHeaders(), "first");

            //Assert
            Assert.Equal(firstDataRequest, actual);
        }
    }
}
