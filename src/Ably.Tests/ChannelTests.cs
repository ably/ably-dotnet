using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Xunit;
using System.Threading;
using Xunit.Extensions;

namespace Ably.Tests
{
    public class ChannelTests : RestApiTests
    {
        [Fact]
        public void Publish_CreatesPostRequestWithValidPath()
        {
            var rest = GetRestClient();
            var channel = rest.Channels.Get("Test");
            channel.Publish("event", "data");

            Assert.Equal(HttpMethod.Post, _currentRequest.Method);
            Assert.Equal(String.Format("/apps/{0}/channels/{1}/publish", rest.Options.AppId, channel.Name), _currentRequest.Url);
        }

        [Fact]
        public void Publish_WithNameAndData_AddsPayloadToRequest()
        {
            var rest = GetRestClient();
            var channel = rest.Channels.Get("Test");
            channel.Publish("event", "data");

            Assert.IsType<ChannelPublishPayload>(_currentRequest.PostData);
            var data = _currentRequest.PostData as ChannelPublishPayload;
            Assert.Equal("data", data.Data);
            Assert.Equal("event", data.Name);
        }

        [Fact]
        public void Publish_WithBinaryArrayData_AddsBase64EncodingToRequest()
        {
            var rest = GetRestClient();
            var channel = rest.Channels.Get("Test");
            channel.Publish("event", new byte[] { 1, 2});

            Assert.IsType<ChannelPublishPayload>(_currentRequest.PostData);
            var postData = _currentRequest.PostData as ChannelPublishPayload;
            Assert.Equal("base64", postData.Encoding);
        }

        [Fact]
        public void History_WithNoOptions_CreateGetRequestWithValidPath()
        {
            var rest = GetRestClient();
            var channel = rest.Channels.Get("Test");
            channel.History();

            Assert.Equal(HttpMethod.Get, _currentRequest.Method);
            Assert.Equal(String.Format("/apps/{0}/channels/{1}/history", rest.Options.AppId, channel.Name), _currentRequest.Url);
        }

        [Fact]
         public void History_WithOptions_AddsParametersToRequest()
        {
            var rest = GetRestClient();
            var channel = rest.Channels.Get("Test");
            var query = new HistoryDataRequestQuery();
            DateTime now = DateTime.Now;
            query.Start = now.AddHours(-1);
            query.End = now;
            query.Direction = QueryDirection.Forwards;
            query.Limit = 1000;
            query.By = HistoryBy.Bundle;
            channel.History(query);

            _currentRequest.AssertContainsParameter("start", query.Start.Value.ToUnixTimeInMilliseconds().ToString());
            _currentRequest.AssertContainsParameter("end", query.End.Value.ToUnixTimeInMilliseconds().ToString());
            _currentRequest.AssertContainsParameter("direction", query.Direction.ToString().ToLower());
            _currentRequest.AssertContainsParameter("limit", query.Limit.Value.ToString());
            _currentRequest.AssertContainsParameter("by", query.By.Value.ToString().ToLower());
        }

        [Theory]
        [InlineData(10001)]
        [InlineData(-1)]
        public void History_WithInvalidLimit_Throws(int limit)
        {
            var rest = GetRestClient();
            var channel = rest.Channels.Get("Test");
            var query = new HistoryDataRequestQuery() { Limit = limit };

            var ex = Assert.Throws<AblyException>(delegate { channel.History(query); });

            Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
        }

        [Theory]
        [PropertyData("InvalidHistoryDates")]
        public void History_WithInvalidStartOrEnd_Throws(DateTime start, DateTime end)
        {
            var rest = GetRestClient();
            var channel = rest.Channels.Get("Test");
            var query = new HistoryDataRequestQuery() { Start = start, End = end };

            var ex = Assert.Throws<AblyException>(delegate { channel.History(query); });

            Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
        }

        public static IEnumerable<object[]> InvalidHistoryDates
        {
            get
            {
                yield return new object[] { new DateTime(1969, 1, 1), DateTime.Now };
                yield return new object[] { new DateTime(2000, 1, 1), new DateTime(1999, 12, 31) };
                yield return new object[] { null, new DateTime(1969, 12, 31) };
            }
        }

        [Fact]
        public void Stats_CreatesGetRequestWithCorrectPath()
        {
            var rest = GetRestClient();
            var channel = rest.Channels.Get("Test");

            channel.Stats();

            Assert.Equal(HttpMethod.Get, _currentRequest.Method);
            Assert.Equal(String.Format("/apps/{0}/channels/{1}/stats", rest.Options.AppId, channel.Name), _currentRequest.Url);
        }

        [Fact]
        public void Stats_WithOptions_AddsParametersToRequest()
        {
            var rest = GetRestClient();
            var channel = rest.Channels.Get("Test");
            var query = new DataRequestQuery();
            DateTime now = DateTime.Now;
            query.Start = now.AddHours(-1);
            query.End = now;
            query.Direction = QueryDirection.Forwards;
            query.Limit = 1000;
            channel.Stats(query);

            _currentRequest.AssertContainsParameter("start", query.Start.Value.ToUnixTimeInMilliseconds().ToString());
            _currentRequest.AssertContainsParameter("end", query.End.Value.ToUnixTimeInMilliseconds().ToString());
            _currentRequest.AssertContainsParameter("direction", query.Direction.ToString().ToLower());
            _currentRequest.AssertContainsParameter("limit", query.Limit.Value.ToString());
        }
    }
}