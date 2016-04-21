using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;
using IO.Ably.Encryption;
using IO.Ably.Rest;
using MsgPack.Serialization;
using Xunit.Abstractions;
using Xunit.Extensions;

namespace IO.Ably.Tests
{
    public class ChannelSpecs : MockHttpSpecs
    {
        [Fact]
        [Trait("spec", "RSN1")]
        public void ChannelsIsACollectionOfChannelObjects()
        {
            var client = GetRestClient();
            (client.Channels is IEnumerable<IChannel>).Should().BeTrue();
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
            public GettingAChannel(ITestOutputHelper output) : base(output)
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
                channel.Publish("event", "data");

                LastRequest.Method.Should().Be(HttpMethod.Post);
                LastRequest.Url.Should().Be($"/channels/{channel.Name}/messages");
                var messages = LastRequest.PostData as List<Message>;
                messages.Should().HaveCount(1);
                messages.First().data.Should().Be("data");
                messages.First().name.Should().Be("event");
                Requests.Should().HaveCount(1);
            }

            [Fact]
            [Trait("spec", "RSL1a")]
            [Trait("spec", "RSL1c")]
            public void WithMessagesList_CreatesOnePostRequestToMessagesRoute()
            {
                var rest = GetRestClient();
                var channel = rest.Channels.Get("Test");
                var message = new Message() { name = "event", data = "data" };
                var message1 = new Message() { name = "event1", data = "data" };
                var message2 = new Message() { name = "event2", data = "data" };
                channel.Publish(new List<Message> { message, message1, message2 });

                Requests.Count.Should().Be(1);

                LastRequest.Method.Should().Be(HttpMethod.Post);
                LastRequest.Url.Should().Be($"/channels/{channel.Name}/messages");
                var postedMessages = LastRequest.PostData as List<Message>;
                postedMessages.Should().HaveCount(3);
                postedMessages.ShouldBeEquivalentTo(new [] { message, message1, message2});
            }

            [Fact]
            [Trait("spec", "RSL1e")]
            public async Task WithNoData_ShouldOnlySendNameProperty()
            {
                var client = GetRestClient();

                var messageWithNoData = new Message() { name = "NoData" };
                await client.Channels.Get("nodata").Publish(messageWithNoData);

                LastRequest.RequestBody.GetText().Should().Be("[{\"name\":\"NoData\"}]");
            }

            [Fact]
            [Trait("spec", "RSL1e")]
            public async Task WithNoName_ShouldOnlySendDataProperty()
            {
                var client = GetRestClient();

                var messageWithNoName = new Message() { data = "NoName" };
                await client.Channels.Get("noname").Publish(messageWithNoName);

                
                LastRequest.RequestBody.GetText().Should().Be("[{\"data\":\"NoName\"}]");
            }

            [Fact(Skip = "Currently the MsgPack serializer does not support skipping properties. Should be available in v0.7")]
            [Trait("spec", "RSL1e")]
            public async Task WithNoNameAndMsgPack_ShouldOnlySendDataProperty()
            {
                var client = GetRestClient(null, opts => opts.UseBinaryProtocol = true);

                var messageWithNoName = new Message() { data = "NoName" };
                await client.Channels.Get("noname").Publish(messageWithNoName);

                Output.WriteLine("Message pack message: " + LastRequest.RequestBody.GetText());
            }

            [Fact]
            public void Publish_WithNameAndData_AddsPayloadToRequest()
            {
                var rest = GetRestClient();
                var channel = rest.Channels.Get("Test");
                channel.Publish("event", "data");

                Assert.IsType<List<Message>>(LastRequest.PostData);
                var messages = LastRequest.PostData as List<Message>;
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

                Assert.IsType<List<Message>>(LastRequest.PostData);
                var postData = (LastRequest.PostData as IList<Message>).First();
                Assert.Equal("base64", postData.encoding);
            }

            

            [Fact]
            public void Publish_WithOneMessage_AddsPayloadToRequest()
            {
                var rest = GetRestClient();
                var channel = rest.Channels.Get("Test");

                var message = new Message() { name = "event", data = "data" };
                channel.Publish(new List<Message> { message });

                var data = LastRequest.PostData as IEnumerable<Message>;
                Assert.NotNull(data);
                Assert.Equal(1, data.Count());
                var payloadMessage = data.First();
                Assert.Equal("data", payloadMessage.data);
                Assert.Equal("event", payloadMessage.name);
            }

            public ChannelPublish(ITestOutputHelper output) : base(output)
            {
            }
        }


        [Fact]
        public void History_WithNoOptions_CreateGetRequestWithValidPath()
        {
            var rest = GetRestClient(request => "[]".ToAblyResponse());
            var channel = rest.Channels.Get("Test");

            channel.History();

            Assert.Equal(HttpMethod.Get, LastRequest.Method);
            Assert.Equal($"/channels/{channel.Name}/messages", LastRequest.Url);
        }

        [Fact]
        public void History_WithOptions_AddsParametersToRequest()
        {
            var rest = GetRestClient(request => "[]".ToAblyResponse());

            var channel = rest.Channels.Get("Test");
            var query = new DataRequestQuery();
            var now = DateTimeOffset.Now;
            query.Start = now.AddHours(-1);
            query.End = now;
            query.Direction = QueryDirection.Forwards;
            query.Limit = 1000;
            channel.History(query);

            LastRequest.AssertContainsParameter("start", query.Start.Value.ToUnixTimeInMilliseconds().ToString());
            LastRequest.AssertContainsParameter("end", query.End.Value.ToUnixTimeInMilliseconds().ToString());
            LastRequest.AssertContainsParameter("direction", query.Direction.ToString().ToLower());
            LastRequest.AssertContainsParameter("limit", query.Limit.Value.ToString());
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
            var rest = GetRestClient(request => new AblyResponse()
            {
                Headers = DataRequestQueryTests.GetSampleHistoryRequestHeaders(),
                TextResponse = "[]"
            }.ToTask());

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
            var message = new Message() { name = "test", data = "Test" };
            var defaultParams = Crypto.GetDefaultParams();

            rest.ExecuteHttpRequest = request =>
            {
                var response = new AblyResponse()
                {
                    Headers = DataRequestQueryTests.GetSampleHistoryRequestHeaders(),
                    TextResponse = $"[{JsonConvert.SerializeObject(message)}]"
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
                yield return new object[] { new DateTimeOffset(1969, 1, 1, 0, 0, 0, TimeSpan.Zero), DateTimeOffset.Now };
                yield return new object[] { new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(1999, 12, 31, 0, 0, 0, TimeSpan.Zero) };
                yield return new object[] { null, new DateTimeOffset(1969, 12, 31, 0, 0, 0, TimeSpan.Zero) };
            }
        }

        [Fact]
        [Trait("spec", "RSN4a")]
        public void Presence_CreatesGetRequestWithCorrectPath()
        {
            var rest = GetRestClient(request => "[]".ToAblyResponse());

            var channel = rest.Channels.Get("Test");

            channel.Presence();

            Assert.Equal(HttpMethod.Get, LastRequest.Method);
            Assert.Equal($"/channels/{channel.Name}/presence", LastRequest.Url);
        }

        public ChannelSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}