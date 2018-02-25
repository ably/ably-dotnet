using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using IO.Ably.Encryption;
using IO.Ably.Rest;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Rest
{
    public class ChannelSpecs : MockHttpRestSpecs
    {
        [Fact]
        [Trait("spec", "RSN1")]
        public void ChannelsIsACollectionOfChannelObjects()
        {
            var client = GetRestClient();
            (client.Channels is IEnumerable<IRestChannel>).Should().BeTrue();
        }

        [Fact]
        [Trait("spec", "RSN2")]
        public void ShouldBeAbleToIterateThroughExistingChannels()
        {
            var client = GetRestClient();
            var channel1 = client.Channels.Get("test");
            var channel2 = client.Channels.Get("test1");

            client.Channels.Should().HaveCount(2);
            client.Channels.ShouldBeEquivalentTo(new[] { channel1, channel2 });
        }

        [Fact]
        [Trait("spec", "RSN2")]
        public void ShouldBeAbleToCheckIsAChannelExists()
        {
            var client = GetRestClient();
            var channel1 = client.Channels.Get("test");
            var channel2 = client.Channels.Get("test1");

            client.Channels.Any(x => x.Name == "test").Should().BeTrue();
        }

        [Trait("spec", "RSN3")]
        public class GettingAChannel : ChannelSpecs
        {
            private AblyRest _client;

            public GettingAChannel(ITestOutputHelper output)
                : base(output)
            {
                _client = GetRestClient();
            }

            [Fact]
            [Trait("spec", "RSN3a")]
            public void WhenChannelDoesntExist_ShouldCreateANewOne()
            {
                var channel = _client.Channels.Get("new");
                channel.Should().NotBeNull();
            }

            [Fact]
            [Trait("spec", "RSN3a")]
            public void WhenChannelAlreadyexists_ShouldReturnExistingChannel()
            {
                var channel = _client.Channels.Get("new");
                var secondTime = _client.Channels.Get("new");
                channel.Should().BeSameAs(secondTime);
            }

            [Fact]
            [Trait("spec", "RSN3a")]
            [Trait("spec", "RSN3b")]
            public void WithProvidedChannelOptions_ShouldSetOptionsOnChannel()
            {
                var options = new ChannelOptions();
                var channel = _client.Channels.Get("test", options);
                (channel as RestChannel).Options.ShouldBeEquivalentTo(options);
            }

            [Fact]
            [Trait("spec", "RSN3c")]
            public void WhenAccesingExistingChannel_WithNewOptions_ShouldUpdateExistingChannelWithNewOptions()
            {
                var channel = _client.Channels.Get("test");
                var initialOptions = (channel as RestChannel).Options;
                var newOptions = new ChannelOptions(true);
                var secondTime = _client.Channels.Get("test", newOptions);
                (secondTime as RestChannel).Options.ShouldBeEquivalentTo(newOptions);
            }
        }

        [Fact]
        [Trait("spec", "RSN4a")]
        public void ShouldBeAbleToReleaseAChannelSoItIsRemovedFromTheChannelsCollection()
        {
            var client = GetRestClient();
            var channel = client.Channels.Get("first");
            client.Channels.Should().Contain(x => x.Name == "first");
            client.Channels.Release("first");
            client.Channels.Should().BeEmpty();
        }

        public class ChannelPublish : ChannelSpecs
        {
            [Fact]
            [Trait("spec", "RSL1a")]
            [Trait("spec", "RSL1b")]
            public void WithNameAndData_CreatesASinglePostRequestWithValidPath()
            {
                var rest = GetRestClient();
                var channel = rest.Channels.Get("Test");
                channel.PublishAsync("event", "data");

                LastRequest.Method.Should().Be(HttpMethod.Post);
                LastRequest.Url.Should().Be($"/channels/{channel.Name}/messages");
                var messages = LastRequest.PostData as List<Message>;
                messages.Should().HaveCount(1);
                messages.First().Data.Should().Be("data");
                messages.First().Name.Should().Be("event");
                Requests.Should().HaveCount(1);
            }

            [Fact]
            [Trait("spec", "RSL1a")]
            [Trait("spec", "RSL1c")]
            public void WithMessagesList_CreatesOnePostRequestToMessagesRoute()
            {
                var rest = GetRestClient();
                var channel = rest.Channels.Get("Test");
                var message = new Message() { Name = "event", Data = "data" };
                var message1 = new Message() { Name = "event1", Data = "data" };
                var message2 = new Message() { Name = "event2", Data = "data" };
                channel.PublishAsync(new List<Message> { message, message1, message2 });

                Requests.Count.Should().Be(1);

                LastRequest.Method.Should().Be(HttpMethod.Post);
                LastRequest.Url.Should().Be($"/channels/{channel.Name}/messages");
                var postedMessages = LastRequest.PostData as List<Message>;
                postedMessages.Should().HaveCount(3);
                postedMessages.ShouldBeEquivalentTo(new[] { message, message1, message2 });
            }

            [Fact]
            [Trait("spec", "RSL1e")]
            public async Task WithNoData_ShouldOnlySendNameProperty()
            {
                var client = GetRestClient();

                var messageWithNoData = new Message() { Name = "NoData" };
                await client.Channels.Get("nodata").PublishAsync(messageWithNoData);

                LastRequest.RequestBody.GetText().Should().Be("[{\"name\":\"NoData\"}]");
            }

            [Fact]
            [Trait("spec", "RSL1e")]
            public async Task WithNoName_ShouldOnlySendDataProperty()
            {
                var client = GetRestClient();

                var messageWithNoName = new Message() { Data = "NoName" };
                await client.Channels.Get("noname").PublishAsync(messageWithNoName);

                LastRequest.RequestBody.GetText().Should().Be("[{\"data\":\"NoName\"}]");
            }

            [Fact]
            [Trait("spec", "RSL1e")]
            public async Task WithBlankMessage_ShouldSendBlankMessage()
            {
                var client = GetRestClient();

                var messageWithNoName = new Message();
                await client.Channels.Get("blank-message").PublishAsync(messageWithNoName);

                LastRequest.RequestBody.GetText().Should().Be("[{}]");
            }

            [Fact]
            [Trait("spec", "RSL1e")]
            public async Task WithNoNameAndMsgPack_ShouldOnlySendDataProperty()
            {
                var client = GetRestClient(null, opts => opts.UseBinaryProtocol = true);

                var messageWithNoName = new Message() { Data = "NoName" };
                await client.Channels.Get("noname").PublishAsync(messageWithNoName);
            }

            [Fact]
            public void Publish_WithNameAndData_AddsPayloadToRequest()
            {
                var rest = GetRestClient();
                var channel = rest.Channels.Get("Test");
                channel.PublishAsync("event", "data");

                Assert.IsType<List<Message>>(LastRequest.PostData);
                var messages = LastRequest.PostData as List<Message>;
                var data = messages.First();
                Assert.Equal("data", data.Data);
                Assert.Equal("event", data.Name);
            }

            [Fact]
            public void Publish_WithBinaryArrayData_AddsBase64EncodingToRequest()
            {
                var rest = GetRestClient();
                var channel = rest.Channels.Get("Test");
                channel.PublishAsync("event", new byte[] { 1, 2 });

                Assert.IsType<List<Message>>(LastRequest.PostData);
                var postData = (LastRequest.PostData as IList<Message>).First();
                Assert.Equal("base64", postData.Encoding);
            }

            [Fact]
            public void Publish_WithOneMessage_AddsPayloadToRequest()
            {
                var rest = GetRestClient();
                var channel = rest.Channels.Get("Test");

                var message = new Message() { Name = "event", Data = "data" };
                channel.PublishAsync(new List<Message> { message });

                var data = LastRequest.PostData as IEnumerable<Message>;
                Assert.NotNull(data);
                data.Count().Should().Be(1);
                var payloadMessage = data.First();
                Assert.Equal("data", payloadMessage.Data);
                Assert.Equal("event", payloadMessage.Name);
            }

            public ChannelPublish(ITestOutputHelper output)
                : base(output)
            {
            }
        }

        public class ChannelHistory : ChannelSpecs
        {
            private AblyRest _client;
            private IRestChannel _channel;

            [Fact]
            [Trait("spec", "RSL2a")]
            public async Task WithNoOptions_CreateGetRequestWithValidPath()
            {
                var result = await _channel.HistoryAsync();

                result.Should().BeOfType<PaginatedResult<Message>>();
                Assert.Equal(HttpMethod.Get, LastRequest.Method);
                Assert.Equal($"/channels/{_channel.Name}/messages", LastRequest.Url);
            }

            [Fact]
            [Trait("spec", "RSL2b")]
            public async Task WithOptions_AddsParametersToRequest()
            {
                var query = new HistoryRequestParams();
                var now = DateTimeOffset.Now;
                query.Start = now.AddHours(-1);
                query.End = now;
                query.Direction = QueryDirection.Forwards;
                query.Limit = 1000;
                await _channel.HistoryAsync(query);

                LastRequest.AssertContainsParameter("start", query.Start.Value.ToUnixTimeInMilliseconds().ToString());
                LastRequest.AssertContainsParameter("end", query.End.Value.ToUnixTimeInMilliseconds().ToString());
                LastRequest.AssertContainsParameter("direction", query.Direction.ToString().ToLower());
                LastRequest.AssertContainsParameter("limit", query.Limit.Value.ToString());
            }

            [Fact]
            [Trait("spec", "RSL2b")]
            public async Task WithStartBeforeEnd_Throws()
            {
                var ex = await Assert.ThrowsAsync<AblyException>(() =>
                        _channel.HistoryAsync(new HistoryRequestParams() { Start = Now, End = Now.AddHours(-1) }));
            }

            [Fact]
            [Trait("spec", "RSL2b2")]
            public async Task WithoutDirection_ShouldDefaultToBackwards()
            {
                await _channel.HistoryAsync();

                LastRequest.AssertContainsParameter("direction", QueryDirection.Backwards.ToString().ToLower());
            }

            [Fact]
            [Trait("spec", "RSL2b3")]
            public async Task WithOutLimit_ShouldUseDefaultOf100()
            {
                await _channel.HistoryAsync();

                LastRequest.AssertContainsParameter("limit", "100");
            }

            [Theory]
            [InlineData(-1)]
            [InlineData(1001)]
            [Trait("spec", "RSL2b3")]
            [Trait("spec", "RSP3a1")]
            public async Task WithLimitLessThan0andMoreThan1000_ShouldThrow(int limit)
            {
                var ex = await
                    Assert.ThrowsAsync<AblyException>(() => _channel.HistoryAsync(new HistoryRequestParams() { Limit = limit }));
            }

            [Fact]
            public async Task History_WithInvalidStartOrEnd_Throws()
            {
                var rest = GetRestClient();
                var channel = rest.Channels.Get("Test");
                foreach (object[] dates in InvalidHistoryDates())
                {
                    var query = new HistoryRequestParams() { Start = (DateTimeOffset?)dates.First(), End = (DateTimeOffset)dates.Last() };

                    var ex = await Assert.ThrowsAsync<AblyException>(async () => await channel.HistoryAsync(query));
                }
            }

            private static IEnumerable<object[]> InvalidHistoryDates()
            {
                    yield return new object[] { new DateTimeOffset(1969, 1, 1, 0, 0, 0, TimeSpan.Zero), DateTimeOffset.Now };
                    yield return new object[] { new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(1999, 12, 31, 0, 0, 0, TimeSpan.Zero) };
                    yield return new object[] { null, new DateTimeOffset(1969, 12, 31, 0, 0, 0, TimeSpan.Zero) };
            }

            [Fact]
            public async Task History_WithPartialResult_ReturnsCorrectFirstCurrentAndNextLinks()
            {
                // Arrange
                var rest = GetRestClient(request => new AblyResponse()
                {
                    Headers = DataRequestQueryTests.GetSampleHistoryRequestHeaders(),
                    TextResponse = "[]"
                }.ToTask());

                var channel = rest.Channels.Get("test");

                // Act
                var result = await channel.HistoryAsync();

                // Assert
                Assert.NotNull(result.NextDataQuery);
                Assert.NotNull(result.CurrentQuery);
                Assert.NotNull(result.FirstDataQuery);
            }

            [Fact]
            public async Task History_ForAnEncryptedChannel_DecryptsMessagesBeforeReturningThem()
            {
                // Arrange
                var rest = GetRestClient();
                var message = new Message() { Name = "test", Data = "Test" };
                var defaultParams = Crypto.GetDefaultParams();

                rest.ExecuteHttpRequest = request =>
                {
                    var response = new AblyResponse()
                    {
                        Headers = DataRequestQueryTests.GetSampleHistoryRequestHeaders(),
                        TextResponse = $"[{JsonHelper.Serialize(message)}]"
                    };
                    return response.ToTask();
                };

                var channel = rest.Channels.Get("test", new ChannelOptions(defaultParams));

                // Act
                var result = await channel.HistoryAsync();

                // Assert
                Assert.NotEmpty(result.Items);
                var firstMessage = result.Items.First();
                Assert.Equal(message.Data, firstMessage.Data);
            }

            public ChannelHistory(ITestOutputHelper output)
                : base(output)
            {
                _client = GetRestClient();
                _channel = _client.Channels.Get("test");
            }
        }

        public ChannelSpecs(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}