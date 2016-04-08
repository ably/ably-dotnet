using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using Xunit;
using IO.Ably.Encryption;
using IO.Ably.Rest;
using Xunit.Extensions;

namespace IO.Ably.Tests
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

            Assert.IsType<List<Message>>(_currentRequest.PostData);
            var messages = _currentRequest.PostData as List<Message>;
            var data = messages.First();
            Assert.Equal("data", data.data);
            Assert.Equal("event", data.name);
        }

        [Fact]
        public void Publish_WithBinaryArrayData_AddsBase64EncodingToRequest()
        {
            var rest = GetRestClient();
            var channel = rest.Channels.Get("Test");
            channel.Publish("event", new byte[] { 1, 2 });

            Assert.IsType<List<Message>>(_currentRequest.PostData);
            var postData = (_currentRequest.PostData as IList<Message>).First();
            Assert.Equal("base64", postData.encoding);
        }

        [Fact]
        public void Publish_WithMessages_CreatesPostRequestToMessagesRoute()
        {
            var rest = GetRestClient();
            var channel = rest.Channels.Get("Test");
            var message = new Message() { name = "event" , data = "data"};
            channel.Publish(new List<Message> {message });

            Assert.Equal(HttpMethod.Post, _currentRequest.Method);
            Assert.Equal(String.Format("/channels/{0}/messages", channel.Name), _currentRequest.Url);
        }

        [Fact]
        public void Publish_WithOneMessage_AddsPayloadToRequest()
        {
            var rest = GetRestClient();
            var channel = rest.Channels.Get("Test");

            var message = new Message() { name = "event", data = "data" };
            channel.Publish(new List<Message> { message });

            var data = _currentRequest.PostData as IEnumerable<Message>;
            Assert.NotNull(data);
            Assert.Equal(1, data.Count());
            var payloadMessage = data.First();
            Assert.Equal("data", payloadMessage.data);
            Assert.Equal("event", payloadMessage.name);
        }

        [Fact]
        public void History_WithNoOptions_CreateGetRequestWithValidPath()
        {
            var rest = GetRestClient();
            var channel = rest.Channels.Get("Test");
            rest.ExecuteHttpRequest = delegate(AblyRequest request)
            {
                _currentRequest = request;
                return "[]".ToAblyResponse();
            };
            channel.History();

            Assert.Equal(HttpMethod.Get, _currentRequest.Method);
            Assert.Equal(String.Format("/channels/{0}/messages", channel.Name), _currentRequest.Url);
        }

        [Fact]
         public void History_WithOptions_AddsParametersToRequest()
        {
            var rest = GetRestClient();
            rest.ExecuteHttpRequest = delegate(AblyRequest request)
            {
                _currentRequest = request;
                return "[]".ToAblyResponse();
            };
            var channel = rest.Channels.Get("Test");
            var query = new DataRequestQuery();
            var now = DateTimeOffset.Now;
            query.Start = now.AddHours(-1);
            query.End = now;
            query.Direction = QueryDirection.Forwards;
            query.Limit = 1000;
            channel.History(query);

            _currentRequest.AssertContainsParameter("start", query.Start.Value.ToUnixTimeInMilliseconds().ToString());
            _currentRequest.AssertContainsParameter("end", query.End.Value.ToUnixTimeInMilliseconds().ToString());
            _currentRequest.AssertContainsParameter("direction", query.Direction.ToString().ToLower());
            _currentRequest.AssertContainsParameter("limit", query.Limit.Value.ToString());
        }

        [Theory]
        [InlineData(10001)]
        [InlineData(-1)]
        public void History_WithInvalidLimit_Throws(int limit)
        {
            var rest = GetRestClient();
            var channel = rest.Channels.Get("Test");
            var query = new DataRequestQuery() { Limit = limit };

            Assert.Throws<AblyException>(delegate { channel.History(query); });
        }

        [Theory]
        [MemberData("InvalidHistoryDates")]
        public void History_WithInvalidStartOrEnd_Throws(DateTimeOffset? start, DateTimeOffset? end)
        {
            var rest = GetRestClient();
            var channel = rest.Channels.Get("Test");
            var query = new DataRequestQuery() { Start = start, End = end };

             Assert.Throws<AblyException>(delegate { channel.History(query); });
        }

        [Fact]
        public void History_WithPartialResult_ReturnsCorrectFirstCurrentAndNextLinks()
        {
            //Arrange
            var rest = GetRestClient();

            rest.ExecuteHttpRequest = request =>
                {
                    var response = new AblyResponse()
                        {
                            Headers = DataRequestQueryTests.GetSampleHistoryRequestHeaders(),
                            TextResponse = "[]"
                        };
                    return response.ToTask();
                };
            var channel = rest.Channels.Get("test");

            //Act
            var result = channel.History().Result;

            //Assert
            Assert.NotNull(result.NextQuery);
            Assert.NotNull(result.CurrentQuery);
            Assert.NotNull(result.FirstQuery);
        }

        [Fact]
        public void History_ForAnEncryptedChannel_DecryptsMessagesBeforeReturningThem()
        {
            //Arrange
            var rest = GetRestClient();
            var message = new Message() {name = "test", data = "Test"};
            var defaultParams = Crypto.GetDefaultParams();

            rest.ExecuteHttpRequest = request =>
            {
                var response = new AblyResponse()
                {
                    Headers = DataRequestQueryTests.GetSampleHistoryRequestHeaders(),
                    TextResponse = string.Format("[{0}]", JsonConvert.SerializeObject(message))
                };
                return response.ToTask();
            };

            var channel = rest.Channels.Get("test", new ChannelOptions(defaultParams));

            //Act
            var result = channel.History().Result;

            //Assert
            Assert.NotEmpty(result);
            var firstMessage = result.First();
            Assert.Equal(message.data, firstMessage.data);
        }

        public static IEnumerable<object[]> InvalidHistoryDates
        {
            get
            {
                yield return new object[] { new DateTimeOffset(1969, 1, 1,0,0,0,TimeSpan.Zero), DateTimeOffset.Now };
                yield return new object[] { new DateTimeOffset(2000, 1, 1, 0,0,0,TimeSpan.Zero), new DateTimeOffset(1999, 12, 31,0,0,0,TimeSpan.Zero) };
                yield return new object[] { null, new DateTimeOffset(1969, 12, 31, 0,0,0,TimeSpan.Zero) };
            }
        }

        [Fact]
        public void Presence_CreatesGetRequestWithCorrectPath()
        {
            var rest = GetRestClient();
            rest.ExecuteHttpRequest = request =>
                {
                    _currentRequest = request;
                    return "[]".ToAblyResponse();
                };
            var channel = rest.Channels.Get("Test");

            channel.Presence();

            Assert.Equal(HttpMethod.Get, _currentRequest.Method);
            Assert.Equal(String.Format("/channels/{0}/presence", channel.Name), _currentRequest.Url);
        }
    }
}