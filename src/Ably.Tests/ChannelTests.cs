using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
            Assert.Equal(String.Format("/channels/{0}/messages", channel.Name), _currentRequest.Url);
        }

        [Fact]
        public void Publish_WithNameAndData_AddsPayloadToRequest()
        {
            var rest = GetRestClient();
            var channel = rest.Channels.Get("Test");
            channel.Publish("event", "data");

            Assert.IsType<Message>(_currentRequest.PostData);
            var data = _currentRequest.PostData as Message;
            Assert.Equal("data", data.Data);
            Assert.Equal("event", data.Name);
        }

        //TODO: Move test to RequestHandlerTests
        //[Fact] 
        //public void Publish_WithBinaryArrayData_AddsBase64EncodingToRequest()
        //{
        //    var rest = GetRestClient();
        //    var channel = rest.Channels.Get("Test");
        //    channel.Publish("event", new byte[] { 1, 2});

        //    Assert.IsType<Message>(_currentRequest.PostData);
        //    var postData = _currentRequest.PostData as Message;
        //    Assert.Equal("base64", postData.Encoding);
        //}

        [Fact]
        public void Publish_WithMessages_CreatesPostRequestToMessagesRoute()
        {
            var rest = GetRestClient();
            var channel = rest.Channels.Get("Test");
            var message = new Message() { Name = "event" , Data = "data"};
            channel.Publish(new List<Message> {message });

            Assert.Equal(HttpMethod.Post, _currentRequest.Method);
            Assert.Equal(String.Format("/channels/{0}/messages", channel.Name), _currentRequest.Url);
        }

        [Fact]
        public void Publish_WithOneMessage_AddsPayloadToRequest()
        {
            var rest = GetRestClient();
            var channel = rest.Channels.Get("Test");

            var message = new Message() { Name = "event", Data = "data" };
            channel.Publish(new List<Message> { message });

            var data = _currentRequest.PostData as IEnumerable<Message>;
            Assert.NotNull(data);
            Assert.Equal(1, data.Count());
            var payloadMessage = data.First();
            Assert.Equal("data", payloadMessage.Data);
            Assert.Equal("event", payloadMessage.Name);
        }

        [Fact]
        public void History_WithNoOptions_CreateGetRequestWithValidPath()
        {
            var rest = GetRestClient();
            var channel = rest.Channels.Get("Test");
            rest.ExecuteRequest = delegate(AblyRequest request)
            {
                _currentRequest = request;
                return new AblyResponse() { TextResponse = "[]"};
            };
            channel.History();

            Assert.Equal(HttpMethod.Get, _currentRequest.Method);
            Assert.Equal(String.Format("/channels/{0}/messages", channel.Name), _currentRequest.Url);
        }

        [Fact]
         public void History_WithOptions_AddsParametersToRequest()
        {
            var rest = GetRestClient();
            rest.ExecuteRequest = delegate(AblyRequest request)
            {
                _currentRequest = request;
                return new AblyResponse() { TextResponse = "[]" };
            };
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

            Assert.Throws<AblyException>(delegate { channel.History(query); });
        }

        [Theory]
        [PropertyData("InvalidHistoryDates")]
        public void History_WithInvalidStartOrEnd_Throws(DateTime start, DateTime end)
        {
            var rest = GetRestClient();
            var channel = rest.Channels.Get("Test");
            var query = new HistoryDataRequestQuery() { Start = start, End = end };

             Assert.Throws<AblyException>(delegate { channel.History(query); });
        }

        [Fact]
        public void History_WithPartialResult_ReturnsCorrectFirstCurrentAndNextLinks()
        {
            //Arrange
            var rest = GetRestClient();
            
            rest.ExecuteRequest = request =>
                {
                    var response = new AblyResponse()
                        {
                            Headers = DataRequestQueryTests.GetSampleHistoryRequestHeaders(),
                            TextResponse = "[]"
                        };
                    return response;
                };
            var channel = rest.Channels.Get("test");
            
            //Act
            var result = channel.History();

            //Assert
            Assert.NotNull(result.NextQuery);
            Assert.NotNull(result.CurrentResultQuery);
            Assert.NotNull(result.InitialResultQuery);
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
        public void Presence_CreatesGetRequestWithCorrectPath()
        {
            var rest = GetRestClient();
            rest.ExecuteRequest = request =>
                {
                    _currentRequest = request;
                    return new AblyResponse() {TextResponse = "[]"};
                };
            var channel = rest.Channels.Get("Test");

            channel.Presence();

            Assert.Equal(HttpMethod.Get, _currentRequest.Method);
            Assert.Equal(String.Format("/channels/{0}/presence", channel.Name), _currentRequest.Url);
        }
    }
}